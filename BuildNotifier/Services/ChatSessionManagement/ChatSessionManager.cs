using BuildNotifier.Data.Models.Bot;
using BuildNotifier.Services.External;
using BuildNotifier.Services.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace BuildNotifier.Services.ChatSessionManagement
{
    /// <summary>
    /// Менеджер для управления жизненным циклом сессий в чатах
    /// </summary>
    public class ChatSessionManager
    {
        private readonly ConcurrentDictionary<string, ChatSessionWithCancellation> _activeSessions = new();
        private readonly IChatSessionFactory _sessionFactory;
        private readonly ILogger<ChatSessionManager> _logger;
        private readonly MessageProducer _messageProducer;

        /// <summary>
        /// Менеджер для управления жизненным циклом сессий в чатах
        /// </summary>
        /// <param name="sessionFactory">Фабрика для создания сессий</param>
        /// <param name="logger">Логгер</param>
        /// <param name="messageProducer">Сервис отправки сообщений в Kafka</param>
        public ChatSessionManager(IChatSessionFactory sessionFactory, ILogger<ChatSessionManager> logger, MessageProducer messageProducer)
        {
            _sessionFactory = sessionFactory;
            _logger = logger;
            _messageProducer = messageProducer;
        }

        /// <summary>
        /// Обрабатывает входящее сообщение для существующей сессии
        /// </summary>
        /// <param name="message">Входящее сообщение</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <remarks>
        /// Отправляет сообщения только в существующую сессию чата
        /// </remarks>
        public async Task ProcessMessageAsync(BotMessage message, CancellationToken cancellationToken)
        {
            if (_activeSessions.TryGetValue(message.Data.ChatId, out var sessionWithCancellation))
            {
                var session = sessionWithCancellation.Session;
                await session.ProcessMessageAsync(message);
            }
            else
            {
                _messageProducer.SendRequest(GetNonexistentSessionResponse(message));
                _logger.LogWarning($"Нет активной сессии для чата: {message.Data.ChatId}.");
            }
        }

        /// <summary>
        /// Создает и запускает новую сессию для чата
        /// </summary>
        /// <param name="message">Сообщение инициализации</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <remarks>
        /// Подписывается на события сессии:
        /// - OnSendMessage: перенаправляет сообщения в messageProducer
        /// - OnSessionEnded: автоматически останавливает сессию
        /// </remarks>
        public async Task CreateAndStartSessionAsync(BotMessage message, CancellationToken cancellationToken)
        {
            var session = _sessionFactory.CreateSession();
            session.OnSendMessage += (botMessage) =>
            {
                _messageProducer.SendRequest(botMessage);
            };
            session.OnSessionEnded += (id) =>
            {
                _ = StopSessionAsync(id);
            };

            ChatSessionWithCancellation chatSessionWithCancellation = new(session);

            if (_activeSessions.TryAdd(message.Data.ChatId, chatSessionWithCancellation))
            {
                var token = chatSessionWithCancellation.CancellationTokenSource.Token;
                await session.StartAsync(token, message);
                _logger.LogInformation($"Запущена сессия для чата: {message.Data.ChatId}");
            }
        }

        /// <summary>
        /// Останавливает и удаляет сессию чата
        /// </summary>
        /// <param name="chatId">Идентификатор чата</param>
        public async Task StopSessionAsync(string chatId)
        {
            if (_activeSessions.TryRemove(chatId, out var sessionWithCancellation))
            {
                var session = sessionWithCancellation.Session;
                try
                {
                    sessionWithCancellation.CancelAndDispose();
                    await session.StopAsync();
                }
                finally
                {
                    if (session is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _logger.LogInformation($"Завершена сессия для чата: {chatId}");
            }
        }


        /// <summary>
        /// Создание ответа на попытку обращения к несуществующей сессии
        /// </summary>
        /// <param name="botMessage">Инициирующее сообщение</param>
        /// <returns>Сериализованное сообщение в формате <see cref="BotMessage"/></returns>
        public static string GetNonexistentSessionResponse(BotMessage botMessage)
        {
            var responseMessage = new BotMessage
            {
                Method = "sendMessage",
                Status = "COMPLETED",
                KafkaMessageId = botMessage.KafkaMessageId,
                Data = new()
                {
                    Text = "Неизвестная команда.",
                    ChatId = botMessage.Data.ChatId,
                    Method = "sendMessage"
                }
            };

            return JsonSerializer.Serialize(responseMessage);
        }
    }
}
