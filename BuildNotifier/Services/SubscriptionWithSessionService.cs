using BuildNotifier.Data.Models.Bot;
using BuildNotifier.Data.Repositories;
using BuildNotifier.Services.Handlers;
using BuildNotifier.Services.Interfaces;
using BuildNotifier.Services.Delegates;
using BuildNotifier.Services.Helpers;
using System.Text.Json;
using System.Threading.Channels;

namespace BuildNotifier.Services
{
    /// <summary>
    /// Сервис управления подписками на уведомления о сборках Bamboo
    /// </summary>
    /// <remarks>
    /// Реализует интерфейсы <see cref="IChatSession"/> и <see cref="IDisposable"/> для управления сессиями подписки/отписки
    /// </remarks>
    public class SubscriptionWithSessionService : IChatSession, IDisposable
    {
        private const int InactivityTimeoutMinutes = 2;
        private const int InactivityCheckIntervalMinutes = 1;

        private readonly PlanChatRepository _planChatRepository;
        private readonly ILogger<SubscriptionWithSessionService> _logger;
        private readonly Channel<BotMessage> _channel = Channel.CreateUnbounded<BotMessage>();
        private readonly SubscribeHandler _subscribeHandler;
        private readonly UnsubscribeHandler _unsubscribeHandler;

        private CancellationToken _cancellationToken;
        private bool _disposed = false;
        private string? _chatId;
        private DateTime _lastActivityTime;
        private string? _lastKafkaMessageId;

        /// <summary>
        /// Идентификатор чата текущей сессии
        /// </summary>
        /// <inheritdoc cref="IChatSession.ChatId"/>
        public string ChatId => _chatId ?? throw new InvalidOperationException("ChatId не инициализирован!");

        /// <inheritdoc cref="IChatSession.OnSendMessage"/>
        public event Action<string>? OnSendMessage;

        /// <inheritdoc cref="IChatSession.OnSessionEnded"/>
        public event Action<string>? OnSessionEnded;

        /// <summary>
        /// Инициализирует новый экземпляр сервиса подписок
        /// </summary>
        /// <param name="planChatRepository">Репозиторий для работы с подписками</param>
        /// <param name="logger">Логгер для записи событий</param>
        public SubscriptionWithSessionService(
            PlanChatRepository planChatRepository,
            ILogger<SubscriptionWithSessionService> logger)
        {
            _planChatRepository = planChatRepository;
            _logger = logger;

            _subscribeHandler = new SubscribeHandler(
                planChatRepository,
                logger,
                SendMessage,
                GetAnswer,
                chatId => OnSessionEnded?.Invoke(chatId));

            _unsubscribeHandler = new UnsubscribeHandler(
                planChatRepository,
                logger,
                SendMessage,
                GetAnswer,
                chatId => OnSessionEnded?.Invoke(chatId));
        }

        /// <inheritdoc cref="IChatSession.ProcessMessageAsync"/>
        public async Task ProcessMessageAsync(BotMessage botMessage)
        {
            await _channel.Writer.WriteAsync(botMessage);
        }

        /// <inheritdoc cref="IChatSession.StartAsync"/>
        public Task StartAsync(CancellationToken cancellationToken, BotMessage initialMessage)
        {
            _cancellationToken = cancellationToken;
            _chatId = initialMessage.Data.ChatId;
            _lastActivityTime = DateTime.Now;

            Task.Run(CheckInactivityAsync, _cancellationToken);

            switch (initialMessage.Data.Text)
            {
                case "/subfailedbuildnotifierwithsession":
                    _subscribeHandler.HandleSubscribe(initialMessage, _cancellationToken).ConfigureAwait(false);
                    break;
                case "/unsubfailedbuildnotifierwithsession":
                    _unsubscribeHandler.HandleUnsubscribe(initialMessage, _cancellationToken).ConfigureAwait(false);
                    break;
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="IChatSession.StopAsync"/>
        public Task StopAsync()
        {
            Dispose();
            return Task.CompletedTask;
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
            string? parseMode = null,
            string? kafkaMessageId = null)
        {
            _lastActivityTime = DateTime.Now;
            _lastKafkaMessageId = kafkaMessageId ?? Guid.NewGuid().ToString();

            var botMessage = new BotMessage
            {
                Method = "sendMessage",
                KafkaMessageId = _lastKafkaMessageId,
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

            OnSendMessage?.Invoke(JsonSerializer.Serialize(botMessage, JsonSettings.DefaultOptions));
        }

        private async Task CheckInactivityAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                if (DateTime.Now - _lastActivityTime >= TimeSpan.FromMinutes(InactivityTimeoutMinutes))
                {
                    SendMessage("Сессия завершилась из-за отсутствия активности.", ChatId,
                        kafkaMessageId: _lastKafkaMessageId);
                    OnSessionEnded?.Invoke(ChatId);
                    break;
                }
                await Task.Delay(TimeSpan.FromMinutes(InactivityCheckIntervalMinutes), _cancellationToken);
            }
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
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