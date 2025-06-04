using BuildNotifier.Data.Models.BambooWebhookPayload;
using BuildNotifier.Data.Models.Bot;
using BuildNotifier.Data.Models.Kafka;
using BuildNotifier.Data.Models.ServiceRegistration;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using BuildNotifier.Services.ChatSessionManagement;
using BuildNotifier.Services.Utilites;

namespace BuildNotifier.Services
{
    /// <summary>
    /// Слушает сообщения от командного менеджера и направляет их на нужные сервисы
    /// </summary>
    public class CommandManagerListenerService : BackgroundService
    {
        private static readonly HashSet<string> SubscriptionWithSessionCommands = new()
        {
            "/subfailedbuildnotifierwithsession",
            "/unsubfailedbuildnotifierwithsession"
        };

        private static readonly HashSet<string> SubscriptionCommands = new()
        {
            "/subfailedbuildnotifier",
            "/unsubfailedbuildnotifier",
            "/myfailedbuildnotifiersubs"
        };

        private readonly ChatSessionManager _sessionManager;
        private readonly TelegramNotificationService _telegramNotificationService;
        private readonly SubscriptionService _subscriptionService;
        private readonly ILogger<CommandManagerListenerService> _logger;
        private readonly IConsumer<Null, string> _consumer;
        private readonly string _messageTopicName;
        private readonly string _hookTopicName;
        private readonly string _serviceName;

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
            SubscriptionService subscriptionService,
            ChatSessionManager sessionManager,
            IOptions<KafkaTopics> kafkaTopics,
            IOptions<ConsumerConfig> consumerOptions,
            ILogger<CommandManagerListenerService> logger
            )
        {
            _telegramNotificationService = telegramNotificationService;
            _subscriptionService = subscriptionService;
            _sessionManager = sessionManager;
            _logger = logger;
            _serviceName = serviceRegistrationInfo?.ServiceName ?? throw new ArgumentNullException(nameof(serviceRegistrationInfo.ServiceName));
            _messageTopicName = serviceRegistrationInfo?.ConsumeTopic ?? throw new ArgumentNullException(nameof(serviceRegistrationInfo.ServiceName));
            _hookTopicName = kafkaTopics.Value.WebhookTopic;

            var config = consumerOptions?.Value;
            _consumer = new ConsumerBuilder<Null, string>(config).Build();
            _consumer.Subscribe(new List<string>(){_messageTopicName, _hookTopicName});
        }

        /// <summary>
        /// Основной цикл обработки входящих сообщений из Kafka.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var kafkaMessage = ConsumeMessage(stoppingToken);
                    if (kafkaMessage == null) continue;

                    await ProcessMessageAsync(kafkaMessage, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Слушатель команд останавливается");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обработке сообщения");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        private ConsumeResult<Null, string>? ConsumeMessage(CancellationToken stoppingToken)
        {
            try
            {
                var kafkaMessage = _consumer.Consume(stoppingToken);
                _logger.LogDebug("Получено сообщение: {Message}", kafkaMessage.Message.Value);
                return kafkaMessage;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Ошибка при получении сообщения из топика {Topic}", _messageTopicName);
                return null;
            }
        }

        private bool TryParseBotMessage(string json, out BotMessage? message)
        {
            if (JsonParser.TryParseJson(json, out message))
                return message != null;

            _logger.LogWarning("Не удалось распарсить BotMessage из JSON: {Json}", json);
            return false;
        }

        private bool TryParseBuildWebhook(string json, out BuildWebhook? payload)
        {
            if (JsonParser.TryParseJson(json, out payload))
                return payload != null;

            _logger.LogWarning("Не удалось распарсить BuildWebhook из JSON: {Json}", json);
            return false;
        }

        private async Task ProcessMessageAsync(ConsumeResult<Null, string> kafkaMessage, CancellationToken stoppingToken)
        {
            var topic = kafkaMessage.Topic;
            var value = kafkaMessage.Message.Value;

            if (topic == _messageTopicName && TryParseBotMessage(value, out var botMessage))
            {
                await ProcessBotMessageAsync(botMessage!, stoppingToken);
                return;
            }

            if (topic == _hookTopicName && TryParseBuildWebhook(value, out var buildPayload))
            {
                if (buildPayload?.ServiceName == _serviceName)
                    ProcessBuildWebhook(buildPayload);
                else
                    _logger.LogInformation("Пойман webhook для другого сервиса: {Message}", value);
                return;
            }

            _logger.LogWarning("Неизвестный формат сообщения: {Message}", value);
        }

        private async Task ProcessBotMessageAsync(BotMessage message, CancellationToken stoppingToken)
        {
            var commandText = message.Data.Text;

            var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var command = parts[0];

            if (SubscriptionCommands.Contains(command))
            {
                var _ = _subscriptionService.ProcessCommand(message.Data.ChatId, commandText, message.KafkaMessageId).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _logger.LogError("Ошибка во время обработки команды.");
                    }
                }, TaskScheduler.Default);
            }
            else if (SubscriptionWithSessionCommands.Contains(command))
            {
                await _sessionManager.CreateAndStartSessionAsync(message);
            }
            else
            {
                await _sessionManager.ProcessMessageAsync(message);
            }
        }

        private void ProcessBuildWebhook(BuildWebhook payload)
        {
            try
            {
                var _ = _telegramNotificationService.NotifyFailedBuildAsync(payload).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _logger.LogError("Ошибка во время обработки вебхука.");
                    }
                }, TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке webhook сборки");
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
