using BuildNotifier.Data.Models.Bot;
using BuildNotifier.Data.Models.ServiceRegistration;
using BuildNotifier.Data.Repositories;
using BuildNotifier.Services.External;
using BuildNotifier.Services.Interfaces;
using System.Text.Json;
using System.Threading.Channels;

namespace BuildNotifier.Services
{
    /// <summary>
    /// Сервис для управления подпиской на уведомления о сборке
    /// </summary>
    public class NotifierSubscriptionService : IChatSession, IDisposable
    {
        private const int InactivityTimeoutMinutes = 2;
        private const int InactivityCheckIntervalMinutes = 1;

        private readonly PlanChatRepository _planChatRepository;
        private readonly ILogger<NotifierSubscriptionService> _logger;
        private readonly Channel<BotMessage> _channel = Channel.CreateUnbounded<BotMessage>();

        private CancellationToken _cancellationToken;
        private bool _disposed;
        private string? _chatId;
        private DateTime _lastActivityTime;

        public string ChatId => _chatId ?? throw new InvalidOperationException("ChatId не инициализирован!");

        public event Action<string>? OnSendMessage;
        public event Action<string>? OnSessionEnded;

        /// <summary>
        /// Инициализирует сервис для управления подпиской на уведомления о сборке
        /// </summary>
        /// <param name="planChatRepository">Репозиторий связей сборок и чатов</param>
        /// <param name="logger">Логгер для вывода информации о внутренних процессах</param>
        public NotifierSubscriptionService(
            PlanChatRepository planChatRepository,
            ILogger<NotifierSubscriptionService> logger)
        {
            _planChatRepository = planChatRepository;
            _logger = logger;
        }

        /// <summary>
        /// Отправляет сообщение в очередь для того, чтобы сервис его прочитал
        /// </summary>
        /// <param name="botMessage">Сообщение от телеграма</param>
        public async Task ProcessMessageAsync(BotMessage botMessage)
        {
            await _channel.Writer.WriteAsync(botMessage);
        }

        /// <summary>
        /// Запускает сервис
        /// </summary>
        /// <param name="cancellationToken">Токен для завершения работы сервиса</param>
        /// <param name="initialMessage">Сообщение, которое инициировало запуск сервиса</param>
        public Task StartAsync(CancellationToken cancellationToken, BotMessage initialMessage)
        {
            _cancellationToken = cancellationToken;
            _chatId = initialMessage.Data.ChatId;
            _lastActivityTime = DateTime.Now;

            Task.Run(CheckInactivityAsync, _cancellationToken);

            switch (initialMessage.Data.Text)
            {
                case "/subfailedbuildnotifier":
                    SubscribeFailedBuildNotifier();
                    break;
                case "/unsubfailedbuildnotifier":
                    UnsubscribeFailedBuildNotifier();
                    break;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Прекращает работу сервиса
        /// </summary>
        public Task StopAsync()
        {
            _cancellationToken.ThrowIfCancellationRequested();
            this.Dispose();
            return Task.CompletedTask;
        }

        private async void SubscribeFailedBuildNotifier()
        {
            try
            {
                SendMessage(
                    "Введите название проекта\\.\nНапишите \\\"`отменить`\\\", если передумали\\.",
                    ChatId,
                    status: "IN_PROGRESS",
                    parseMode: "MarkdownV2");

                while (!_cancellationToken.IsCancellationRequested)
                {
                    var botMessage = await GetAnswer();
                    if (botMessage == null) continue;

                    if (botMessage.Data.Text.ToLower() == "отменить")
                    {
                        SendMessage("Сессия подписки завершена.", ChatId, status: "COMPLETED");
                        return;
                    }

                    if (!ValidateProjectName(botMessage.Data.Text))
                    {
                        SendMessage(
                            $"Некорректное название проекта \"{botMessage.Data.Text}\". Введите корректное.\nПример: \"PROJ-PLAN\"",
                            ChatId,
                            status: "IN_PROGRESS");
                        continue;
                    }

                    var isSaved = _planChatRepository.AddPlanChat(botMessage.Data.Text, ChatId);
                    var message = isSaved
                        ? $"Подписка на уведомления от проекта \"{botMessage.Data.Text}\" оформлена."
                        : $"Вы уже подписаны на \"{botMessage.Data.Text}\".";

                    SendMessage(message, ChatId, status: isSaved ? "COMPLETED" : "IN_PROGRESS");

                    if (isSaved) return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при подписке");
                SendMessage("Что-то пошло не так.", ChatId, status: "COMPLETED");
            }
            finally
            {
                OnSessionEnded?.Invoke(ChatId);
            }
        }

        private async void UnsubscribeFailedBuildNotifier()
        {
            try
            {
                var planNames = _planChatRepository.GetPlanNames(ChatId);
                if (!planNames.Any())
                {
                    SendMessage("У вас нет активных подписок.", ChatId);
                    return;
                }

                var formattedPlanNames = planNames.Select(p => "\\- \\\"" + TelegramMarkdownHelper.GetMono(p) + "\\\"").ToList();
                var instruction =
                    "Чтобы удалить подписку напишите название плана проекта\n" +
                    "Чтобы удалить все проекты \\- напишите *\\\"`удалить все`\\\"*\n" +
                    "Чтобы завершить сессию отписки \\- напишите *\\\"`завершить`\\\"*";

                var text =
                    "Ниже список планов проектов: \n" +
                    string.Join("\n", formattedPlanNames) + "\n" +
                    instruction;

                SendMessage(text, ChatId, status: "IN_PROGRESS", parseMode: "MarkdownV2");

                while (!_cancellationToken.IsCancellationRequested)
                {
                    var botMessage = await GetAnswer();
                    if (botMessage == null) continue;

                    switch (botMessage.Data.Text.ToLower())
                    {
                        case "удалить все":
                            if (_planChatRepository.DeleteAllPlansFromChat(ChatId))
                            {
                                SendMessage("Вы успешно отписались от всех проектов.", ChatId, status: "COMPLETED");
                            }
                            else
                            {
                                SendMessage("Проекты не найдены.", ChatId, status: "IN_PROGRESS");
                            }
                            return;

                        case "завершить":
                            return;

                        default:
                            if (_planChatRepository.DeletePlanChat(botMessage.Data.Text, ChatId))
                            {
                                SendMessage($"Вы успешно отписались от {botMessage.Data.Text}.", ChatId, status: "IN_PROGRESS");
                            }
                            else
                            {
                                SendMessage($"Проект под названием \\\"{botMessage.Data.Text}\\\" не найден.", ChatId, status: "IN_PROGRESS");
                            }
                            break;
                    }
                }
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Таймаут сессии отписки");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отписке");
                SendMessage("Произошла ошибка.", ChatId);
            }
            finally
            {
                SendMessage("Сессия отписки завершена.", ChatId);
                OnSessionEnded?.Invoke(ChatId);
            }
        }

        private async Task<BotMessage?> GetAnswer()
        {
            var answer = await _channel.Reader.ReadAsync(_cancellationToken);
            _lastActivityTime = DateTime.Now;
            return answer;
        }

        private void SendMessage(
            string message,
            string chatId,
            string status = "COMPLETED",
            InlineKeyboardMarkup? inlineKeyboardMarkup = null,
            string? parseMode = null)
        {
            _lastActivityTime = DateTime.Now;

            var botMessage = new BotMessage
            {
                Method = "sendMessage",
                KafkaMessageId = Guid.NewGuid().ToString(),
                Status = status,
                Data = new()
                {
                    Text = message,
                    ChatId = chatId,
                    Method = "sendMessage",
                    ParseMode = parseMode,
                    ReplyMarkup = inlineKeyboardMarkup
                }
            };

            OnSendMessage?.Invoke(JsonSerializer.Serialize(botMessage));
        }

        private bool ValidateProjectName(string projectName)
        {
            return BambooKeyValidator.IsValidProjectPlanKey(projectName);
        }

        private async Task CheckInactivityAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(InactivityCheckIntervalMinutes), _cancellationToken);

                if (DateTime.Now - _lastActivityTime >= TimeSpan.FromMinutes(InactivityTimeoutMinutes))
                {
                    OnSessionEnded?.Invoke(ChatId);
                    break;
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            OnSessionEnded = null;
            OnSendMessage = null;
            _disposed = true;

            GC.SuppressFinalize(this);
        }
    }
}