using BuildNotifier.Data.Models.Bot;
using BuildNotifier.Data.Repositories;
using BuildNotifier.Services.Delegates;
using BuildNotifier.Services.Helpers;

namespace BuildNotifier.Services.Handlers
{
    /// <summary>
    /// Обработчик подписки на уведомления о сборках Bamboo
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр класса SubscribeHandler
    /// </remarks>
    /// <param name="planChatRepository">Репозиторий для работы с подписками на планы сборки</param>
    /// <param name="logger">Логгер для записи событий</param>
    /// <param name="sendMessage">Делегат для отправки сообщений</param>
    /// <param name="getAnswer">Функция получения ответа от пользователя</param>
    /// <param name="onSessionEnded">Действие, вызываемое при завершении сессии</param>
    public class SubscribeHandler(
        PlanChatRepository planChatRepository,
        ILogger logger,
        CustomDelegates.SendMessageDelegate sendMessage,
        Func<Task<BotMessage?>> getAnswer,
        Action<string> onSessionEnded)
    {
        private readonly PlanChatRepository _planChatRepository = planChatRepository;
        private readonly ILogger _logger = logger;
        private readonly CustomDelegates.SendMessageDelegate _sendMessage = sendMessage;
        private readonly Func<Task<BotMessage?>> _getAnswer = getAnswer;
        private readonly Action<string> _onSessionEnded = onSessionEnded;

        /// <summary>
        /// Обрабатывает процесс подписки на уведомления о сборках
        /// </summary>
        /// <param name="initialMessage">Начальное сообщение, инициировавшее подписку</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>Task, представляющий асинхронную операцию</returns>
        public async Task HandleSubscribe(BotMessage initialMessage, CancellationToken cancellationToken)
        {
            try
            {
                SendInitialMessage(initialMessage);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = await _getAnswer();
                    if (message == null) continue;

                    if (message.Data.Text.Equals("close_failed_build_notifier_session", StringComparison.CurrentCultureIgnoreCase))
                    {
                        HandleSessionClose(message);
                        return;
                    }

                    if (!ValidateProjectName(message.Data.Text))
                    {
                        SendInvalidProjectMessage(message);
                        continue;
                    }

                    var isSaved = await _planChatRepository.AddPlanChatAsync(message.Data.Text, message.Data.ChatId);
                    HandleSubscriptionResult(message, isSaved);

                    if (isSaved) return;
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Операция отменена.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при подписке");
                _sendMessage("Что-то пошло не так.",
                    initialMessage.Data.ChatId,
                    status: "COMPLETED",
                    kafkaMessageId: initialMessage.KafkaMessageId);
            }
            finally
            {
                _onSessionEnded(initialMessage.Data.ChatId);
            }
        }

        private void SendInitialMessage(BotMessage message)
        {
            _sendMessage(
                "Введите название плана сборки в формате " +
                "\\\"[Project \\- Plan](https://drive.google.com/file/d/1uZR6zDXhMe19hd1Y0OfhoxPExIiiGQyg/view?usp=sharing)\\\" " +
                "\\([?](https://confluence.atlassian.com/bamboo/bamboo-variables-289277087.html#:~:text=Some%20job%20name-,bamboo.planName,-The%20current%20plan%27s)\\) \\(Без кавычек\\)",
                message.Data.ChatId,
                status: "IN_PROGRESS",
                parseMode: "MarkdownV2",
                kafkaMessageId: message.KafkaMessageId,
                inlineKeyboardMarkup: new InlineKeyboardMarkup
                {
                    InlineKeyboardButtons =
                    [
                        [new InlineKeyboardButton { Text = "Отменить", CallbackData = "close_failed_build_notifier_session" }]
                    ]
                });
        }

        private void SendInvalidProjectMessage(BotMessage message)
        {
            _sendMessage(
                $"Некорректное название плана сборки \"{message.Data.Text}\". Введите корректное.\nПример: \"Project - Plan\"",
                message.Data.ChatId,
                status: "IN_PROGRESS",
                kafkaMessageId: message.KafkaMessageId,
                inlineKeyboardMarkup: new InlineKeyboardMarkup
                {
                    InlineKeyboardButtons =
                    [
                        [new InlineKeyboardButton { Text = "Отменить", CallbackData = "close_failed_build_notifier_session" }]
                    ]
                });
        }

        private void HandleSessionClose(BotMessage message)
        {
            _sendMessage("Сессия подписки завершена.",
                message.Data.ChatId,
                status: "COMPLETED",
                kafkaMessageId: message.KafkaMessageId);
        }

        private void HandleSubscriptionResult(BotMessage message, bool isSaved)
        {
            var planName = TelegramMarkdownHelper.EscapeMarkdownV2(message.Data.Text);

            var response = isSaved
                ? $"Подписка на уведомления о сборках \\\"`{planName}`\\\" оформлена\\."
                : $"Вы уже подписаны на \\\"`{planName}`\\\"\\.";

            _sendMessage(response,
                message.Data.ChatId,
                status: isSaved ? "COMPLETED" : "IN_PROGRESS",
                kafkaMessageId: message.KafkaMessageId,
                parseMode: "MarkdownV2",
                inlineKeyboardMarkup: !isSaved ?
                new InlineKeyboardMarkup
                {
                    InlineKeyboardButtons =
                    [
                        [new InlineKeyboardButton { Text = "Отменить", CallbackData = "close_failed_build_notifier_session" }]
                    ]
                }
                : null
                );
        }

        private static bool ValidateProjectName(string projectName)
        {
            return BambooValidator.IsValidProjectPlanName(projectName);
        }
    }
}
