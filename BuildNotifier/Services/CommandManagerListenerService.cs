using BuildNotifier.Data.Models.BambooWebhookPayload;
using BuildNotifier.Data.Models.Bot;
using BuildNotifier.Data.Models.ServiceRegistration;
using BuildNotifier.Services.External;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace BuildNotifier.Services
{
    /// <summary>
    /// Слушает сообщения от командного менеджера и направляет их на нужные сервисы
    /// </summary>
    public class CommandManagerListenerService : BackgroundService
    {
        private static readonly HashSet<string> SessionCommands = new()
        {
            "/subfailedbuildnotifier",
            "/unsubfailedbuildnotifier"
        };

        private readonly ChatSessionManager _sessionManager;
        private readonly TelegramNotificationService _telegramNotificationService;
        private readonly ILogger<CommandManagerListenerService> _logger;
        private readonly IConsumer<Null, string> _consumer;
        private readonly string _topicName;

        /// <summary>
        /// Инициализирует сервис прослушивания сообщений из командного менеджера
        /// </summary>
        /// <param name="serviceRegistrationInfo">Информация о доступных топиках для обмена сообщений с командным менеджером</param>
        /// <param name="telegramNotificationService">Сервис для отправки уведомлений об упавших сборках</param>
        /// <param name="sessionManager">Менеджер для управления сессиями в чатах</param>
        /// <param name="consumerOptions">Конфигурация consumer для Kafka</param>
        /// <param name="logger">Логгер для вывода информации о внутренних процессах</param>
        /// <exception cref="ArgumentNullException">Обязательные параметры не заданы</exception>
        public CommandManagerListenerService(
            ServiceRegistrationInfo serviceRegistrationInfo,
            TelegramNotificationService telegramNotificationService,
            ChatSessionManager sessionManager,
            IOptions<ConsumerConfig> consumerOptions,
            ILogger<CommandManagerListenerService> logger
            )
        {
            _telegramNotificationService = telegramNotificationService ?? throw new ArgumentNullException(nameof(telegramNotificationService));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var config = consumerOptions?.Value ?? throw new ArgumentNullException(nameof(consumerOptions));
            _consumer = new ConsumerBuilder<Null, string>(config).Build();
            _topicName = serviceRegistrationInfo?.ConsumeTopic ?? throw new ArgumentNullException(nameof(serviceRegistrationInfo));
            _consumer.Subscribe(_topicName);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = ConsumeMessageWithTimeout(stoppingToken);
                    if (message == null) continue;

                    await ProcessMessageAsync(message.Message.Value, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Command listener is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        private ConsumeResult<Null, string>? ConsumeMessageWithTimeout(CancellationToken stoppingToken)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                _logger.LogDebug("Получено сообщение: {Message}", result.Message.Value);
                return result;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Error consuming message from topic {Topic}", _topicName);
                return null;
            }
        }

        private async Task ProcessMessageAsync(string messageJson, CancellationToken stoppingToken)
        {
            if (TryParseBotMessage(messageJson, out var botMessage))
            {
                await ProcessBotMessageAsync(botMessage!, stoppingToken);
                return;
            }

            if (TryParseBuildWebhook(messageJson, out var buildPayload))
            {
                ProcessBuildWebhook(buildPayload!);
                return;
            }

            _logger.LogWarning("Unknown message format: {Message}", messageJson);
        }

        private bool TryParseBotMessage(string json, out BotMessage? message)
        {
            if (JsonParser.TryParseJson(json, out message))
                return message != null;

            _logger.LogWarning("Failed to parse BotMessage from JSON: {Json}", json);
            return false;
        }

        private bool TryParseBuildWebhook(string json, out BuildWebhook? payload)
        {
            if (JsonParser.TryParseJson(json, out payload))
                return payload != null;

            _logger.LogWarning("Failed to parse BuildWebhook from JSON: {Json}", json);
            return false;
        }

        private async Task ProcessBotMessageAsync(BotMessage message, CancellationToken stoppingToken)
        {
            var command = message.Data.Text.ToLowerInvariant();

            if (SessionCommands.Contains(command))
            {
                await _sessionManager.CreateAndStartSessionAsync(message, stoppingToken);
            }
            else
            {
                await _sessionManager.ProcessMessageAsync(message, stoppingToken);
            }
        }

        private void ProcessBuildWebhook(BuildWebhook payload)
        {
            try
            {
                _telegramNotificationService.NotifyFailedBuildAsync(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing build webhook");
            }
        }

        public override void Dispose()
        {
            _consumer.Close();
            _consumer.Dispose();
            base.Dispose();
        }
    }
}
