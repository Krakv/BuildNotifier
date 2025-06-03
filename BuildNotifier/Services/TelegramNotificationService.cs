using static BuildNotifier.Services.Helpers.TelegramMarkdownHelper;
using BuildNotifier.Services.External;
using BuildNotifier.Data.Models.BambooWebhookPayload;
using BuildNotifier.Data.Models.Bot;
using BuildNotifier.Data.Models.HTTPClient;
using BuildNotifier.Data.Models.ServiceRegistration;
using BuildNotifier.Data.Repositories;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using BuildNotifier.Services.Helpers;

namespace BuildNotifier.Services
{
    /// <summary>
    /// Сервис для отправки данных о build из Webhook в чат телеграма
    /// </summary>
    public class TelegramNotificationService
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly PlanChatRepository _planChatRepository;
        private readonly ApiHttpClient _apiHttpClient;
        private readonly ILogger<TelegramNotificationService> _logger;
        private readonly MessageProducer _messageProducer;
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
            MessageProducer messageProducer,
            IOptions<TelegramNicknameService> apiOptions,
            IOptions<JsonSerializerOptions> jsonOptions,
            PlanChatRepository planChatRepository,
            ILogger<TelegramNotificationService> logger,
            ApiHttpClient apiHttpClient)
        {
            _jsonOptions = jsonOptions.Value;
            _messageProducer = messageProducer;
            _planChatRepository = planChatRepository;
            _logger = logger;
            _apiHttpClient = apiHttpClient;
            _apiUrl = apiOptions.Value.ApiUrl;
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

            var buildKey = BambooValidator.TrimToProjectPlanName(webhookData.Build.BuildPlanName);
            var chatIds = await _planChatRepository.GetChatIdsAsync(buildKey);

            if (!chatIds.Any())
            {
                _logger.LogInformation($"Нет подписчиков на сборку {buildKey}");
                return;
            }

            var messageText = await BuildNotificationMessage(webhookData);

            foreach (var chatId in chatIds)
            {
                var botMessage = CreateBotMessage(chatId, messageText);
                SendKafkaNotification(botMessage);
            }
        }

        private async Task<string> BuildNotificationMessage(BuildWebhook webhookData)
        {
            var authorLogin = BambooValidator.RemoveEmail(webhookData.Commit.Author);
            var username = await FindTelegramUsernameByLogin(authorLogin);
            if (username != "")
            {
                webhookData.Commit.Author = "@" + username;
            }

            return FormatTelegramMessage(webhookData);
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

        private void SendKafkaNotification(BotMessage message)
        {
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            _messageProducer.SendRequest(json);
        }

        private async Task<string> FindTelegramUsernameByLogin(string login)
        {
            return await _apiHttpClient.GetStringResponseAsync(_apiUrl, login);
        }

        private static string FormatTelegramMessage(BuildWebhook notification)
        {
            var builder = new StringBuilder();

            string commitUrl = notification.RepositoryUrl.Contains("github.com")
                ? $"{notification.RepositoryUrl}/commit/{notification.Commit.Hash}"
                : $"{notification.RepositoryUrl}/commits/{notification.Commit.Hash}";

            builder.AppendLine($"*🚨Сборка {(notification.Build.Status == "FAILED" ? "упала" : "успешна")}*: *{EscapeMarkdownV2(notification.Build.BuildPlanName)}*");
            builder.AppendLine($"**>Последний коммит:");
            builder.AppendLine($">🧑‍💻Автор: {EscapeMarkdownV2(notification.Commit.Author)}");
            builder.AppendLine($">🌿Ветка: `{EscapeMarkdownV2(notification.BranchName)}`");
            builder.AppendLine($">🔗Коммит: [{EscapeMarkdownV2(ShortenCommitHash(notification.Commit.Hash))}]({commitUrl})");

            if (!string.IsNullOrWhiteSpace(notification.Commit.Message))
            {
                builder.AppendLine($">📝Сообщение: `{EscapeMarkdownV2(notification.Commit.Message.Trim())}`");
            }
            builder.AppendLine($">");
            builder.AppendLine($">🔨Сборка:");
            builder.AppendLine($">🔑Ключ сборки: `{EscapeMarkdownV2(notification.Build.BuildResultKey)}` \\(*{EscapeMarkdownV2(notification.Build.Status)}*\\)");
            builder.AppendLine($">🕒Время: `{EscapeMarkdownV2(notification.Time)}`");
            builder.AppendLine($">||");

            return builder.ToString();
        }

        private static string ShortenCommitHash(string hash)
        {
            return hash.Length > 7 ? hash.Substring(0, 7) : hash;
        }
    }
}