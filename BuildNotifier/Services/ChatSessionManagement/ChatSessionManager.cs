using BuildNotifier.Data.Models.Bot;
using BuildNotifier.Services.External;
using BuildNotifier.Services.Helpers;
using BuildNotifier.Services.Interfaces;
using Confluent.Kafka;
using System.Collections.Concurrent;
using System.Text.Json;

namespace BuildNotifier.Services.ChatSessionManagement
{
    /// <summary>
    /// Менеджер для управления жизненным циклом сессий в чатах
    /// </summary>
    /// <remarks>
    /// Менеджер для управления жизненным циклом сессий в чатах
    /// </remarks>
    /// <param name="sessionFactory">Фабрика для создания сессий</param>
    /// <param name="logger">Логгер</param>
    /// <param name="messageProducer">Сервис отправки сообщений в Kafka</param>
    public class ChatSessionManager(IChatSessionFactory sessionFactory, ILogger<ChatSessionManager> logger, MessageProducer messageProducer)
    {
        private readonly ConcurrentDictionary<string, ChatSessionWithCancellation> _activeSessions = new();
        private readonly IChatSessionFactory _sessionFactory = sessionFactory;
        private readonly ILogger<ChatSessionManager> _logger = logger;
        private readonly MessageProducer _messageProducer = messageProducer;

        /// <summary>
        /// Обрабатывает входящее сообщение для существующей сессии
        /// </summary>
        /// <param name="message">Входящее сообщение</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <remarks>
        /// Отправляет сообщения только в существующую сессию чата
        /// </remarks>
        public async Task ProcessMessageAsync(BotMessage message)
        {
            if (_activeSessions.TryGetValue(message.Data.ChatId, out var sessionWithCancellation))
            {
                var session = sessionWithCancellation.Session;
                await session.ProcessMessageAsync(message);
            }
            else
            {
                await _messageProducer.SendRequest(GetNonexistentSessionResponse(message));
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
        public async Task CreateAndStartSessionAsync(BotMessage message, CancellationToken stoppingToken)
        {
            var session = _sessionFactory.CreateSession();
            session.OnSendMessage += SendRequestToKafka;
            session.OnSessionEnded += (id) =>
            {
                _ = StopSessionAsync(id);
            };

            ChatSessionWithCancellation chatSessionWithCancellation = new(session);

            if (_activeSessions.TryAdd(message.Data.ChatId, chatSessionWithCancellation))
            {
                var sessionToken = chatSessionWithCancellation.CancellationTokenSource.Token;
                // Отменяется в двух случаях:
                // 1. Отменена работа конкретной сессии
                // 2. Отменена работа всего приложения
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(sessionToken, stoppingToken);

                await session.StartAsync(linkedCts.Token, message);
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

            return JsonSerializer.Serialize(responseMessage, JsonSettings.DefaultOptions);
        }

        private async void SendRequestToKafka(string botMessage)
        {
            await _messageProducer.SendRequest(botMessage);
        }
    }
}
