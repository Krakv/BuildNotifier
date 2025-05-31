using BuildNotifier.Data.Models.BambooWebhookPayload;
using BuildNotifier.Data.Models.Bot;
using BuildNotifier.Data.Models.ServiceRegistration;
using BuildNotifier.Data.Models.HTTPClient;
using BuildNotifier.Data.Repositories;
using BuildNotifier.Services.External;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BuildNotifier.Services
{
    /// <summary>
    /// Сервис для отправки данных о build из Webhook в чат телеграма
    /// </summary>
    public class TelegramNotificationService
    {
        private readonly ProducerConfig _producerConfig;
        private readonly ServiceRegistrationInfo _serviceRegistrationInfo;
        private readonly PlanChatRepository _planChatRepository;
        private readonly ApiHttpClient _apiHttpClient;
        private readonly ILogger<TelegramNotificationService> _logger;
        private readonly IProducer<Null, string> _producer;
        private readonly string _apiUrl;

        /// <summary>
        /// Инициализирует сервис для отправки данных о build из Webhook в чат телеграма
        /// </summary>
        /// <param name="producerOptions">Конфигурация producer для Kafka</param>
        /// <param name="httpOptions">Конфигурация, которая содержит URL адрес api, где можно получить username в телеграме по логину доменной учетной записи</param>
        /// <param name="serviceRegistrationInfo">Информация о доступных топиках для обмена сообщений с командным менеджером</param>
        /// <param name="planChatRepository">Репозиторий связей сборок и чатов</param>
        /// <param name="logger">Логгер для вывода информации о внутренних процессах</param>
        /// <param name="apiHttpClient">Клиент для отправки запроса по http</param>
        public TelegramNotificationService(
            IOptions<ProducerConfig> producerOptions,
            IOptions<HttpUrl> httpOptions,
            ServiceRegistrationInfo serviceRegistrationInfo,
            PlanChatRepository planChatRepository,
            ILogger<TelegramNotificationService> logger,
            ApiHttpClient apiHttpClient)
        {
            _producerConfig = producerOptions.Value;
            _serviceRegistrationInfo = serviceRegistrationInfo;
            _planChatRepository = planChatRepository;
            _logger = logger;
            _apiHttpClient = apiHttpClient;
            _apiUrl = httpOptions.Value.Url;
            _producer = new ProducerBuilder<Null, string>(_producerConfig).Build();
        }

        /// <summary>
        /// Уведомляет в чат об упавшей сборке
        /// </summary>
        /// <param name="webhookData">Информация о сборке</param>
        public async void NotifyFailedBuildAsync(BuildWebhook webhookData)
        {
            if (string.IsNullOrEmpty(webhookData.Uuid))
            {
                _logger.LogWarning("Получен вебхук с пустым UUID");
                return;
            }

            var buildKey = BambooKeyValidator.TrimBuildNumber(webhookData.Build.BuildResultKey);
            var chatIds = _planChatRepository.GetChatIds(buildKey);

            if (!chatIds.Any())
            {
                _logger.LogInformation($"Нет подписчиков на сборку {buildKey}");
                return;
            }

            webhookData = await ReplaceLoginWithTelegramUsername(webhookData);

            var messageText = BuildNotificationMessage(webhookData);

            foreach (var chatId in chatIds)
            {
                var botMessage = CreateBotMessage(chatId, messageText);
                await SendKafkaNotificationAsync(botMessage);
            }
        }

        private string BuildNotificationMessage(BuildWebhook webhookData)
        {
            return $"""
                🚨*Сборка упала {TelegramMarkdownHelper.EscapeMarkdownV2(webhookData.Build.BuildResultKey)}*
                **>👤Автор коммита: {TelegramMarkdownHelper.EscapeMarkdownV2(webhookData.Commit.Author)}
                >⏰Ветка: `{TelegramMarkdownHelper.EscapeMarkdownV2(webhookData.BranchName)}`
                >🔗Репозиторий: {TelegramMarkdownHelper.EscapeMarkdownV2(webhookData.RepositoryUrl)}
                >📌Хеш коммита: `{TelegramMarkdownHelper.EscapeMarkdownV2(webhookData.Commit.Hash)}`
                >💬Комментарий коммита: `{TelegramMarkdownHelper.EscapeMarkdownV2(webhookData.Commit.Message)}` ||
                """;
        }

        private BotMessage CreateBotMessage(string chatId, string messageText)
        {
            return new BotMessage
            {
                Method = "sendMessage",
                KafkaMessageId = Guid.NewGuid().ToString(),
                Status = "COMPLETED",
                Data = new()
                {
                    Text = messageText,
                    ChatId = chatId,
                    Method = "sendMessage",
                    ParseMode = "MarkdownV2"
                }
            };
        }

        private async Task SendKafkaNotificationAsync(BotMessage message)
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                var kafkaMessage = new Message<Null, string> { Value = json };

                var deliveryResult = await _producer.ProduceAsync(
                    _serviceRegistrationInfo.ProduceTopic,
                    kafkaMessage);

                _logger.LogDebug($"[{_serviceRegistrationInfo.ProduceTopic}] Сообщение доставлено: {deliveryResult.Message.Value}");
            }
            catch (ProduceException<Null, string> ex)
            {
                _logger.LogError(ex, $"[{_serviceRegistrationInfo.ProduceTopic}] Не удалось доставить сообщение в Kafka");
            }
        }

        private async Task<BuildWebhook> ReplaceLoginWithTelegramUsername(BuildWebhook buildWebhook)
        {
            var username = await GetDataFromApiAsync(buildWebhook.Commit.Author);
            if (username != "") buildWebhook.Commit.Author = username;
            return buildWebhook;
        }

        private async Task<string> GetDataFromApiAsync(string username)
        {
            return await _apiHttpClient.GetStringResponseAsync(_apiUrl, username);
        }
    }
}