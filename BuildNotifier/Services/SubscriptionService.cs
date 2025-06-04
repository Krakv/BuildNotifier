using BuildNotifier.Data.Models.Bot;
using BuildNotifier.Data.Repositories;
using BuildNotifier.Services.External;
using BuildNotifier.Services.Helpers;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text;

namespace BuildNotifier.Services
{
    /// <summary>
    /// Сервис для управления подписками на уведомления о сборках в Bamboo
    /// </summary>
    /// <remarks>
    /// Конструктор сервиса подписок
    /// </remarks>
    /// <param name="planChatRepository">Репозиторий для работы с подписками</param>
    /// <param name="logger">Логгер</param>
    /// <param name="messageProducer">Сервис для отправки сообщений</param>
    public class SubscriptionService(
        PlanChatRepository planChatRepository,
        ILogger<SubscriptionService> logger,
        MessageProducer messageProducer)
    {
        private readonly PlanChatRepository _planChatRepository = planChatRepository;
        private readonly ILogger<SubscriptionService> _logger = logger;
        private readonly MessageProducer _messageProducer = messageProducer;

        /// <summary>
        /// Обрабатывает входящую команду от пользователя
        /// </summary>
        /// <param name="chatId">ID чата Telegram</param>
        /// <param name="commandText">Текст команды</param>
        /// <param name="kafkaMessageId">ID сообщения Kafka</param>
        public async void ProcessCommand(string chatId, string commandText, string kafkaMessageId)
        {
            var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLowerInvariant();
            var parameters = string.Join(" ", parts.Skip(1));

            switch (command)
            {
                case "/subfailedbuildnotifier":
                    await ProcessSubscriptionCommand(chatId, parameters, kafkaMessageId);
                    break;
                case "/unsubfailedbuildnotifier":
                    await ProcessUnsubscriptionCommand(chatId, parameters, kafkaMessageId);
                    break;
                case "/myfailedbuildnotifiersubs":
                    await ProcessListSubscriptionsCommand(chatId, kafkaMessageId);
                    break;
            }
        }

        /// <summary>
        /// Обрабатывает команду подписки на уведомления о неудачных сборках
        /// </summary>
        /// <param name="chatId">ID чата в Telegram</param>
        /// <param name="parameters">Параметры команды</param>
        /// <param name="kafkaMessageId">ID сообщения Kafka</param>
        public async Task ProcessSubscriptionCommand(string chatId, string parameters, string kafkaMessageId)
        {
            try
            {
                if (parameters == "")
                {
                    await SendSubscribeHelpMessage(chatId, kafkaMessageId);
                    return;
                }

                var planNames = ParsePlanParameters(parameters);

                if (planNames.Count == 0)
                {
                    await SendResponse(chatId, "Не указаны планы сборки для подписки\\. Формат: Проект \\- План", kafkaMessageId);
                    return;
                }

                var addedPlans = new List<string>();
                var existingPlans = new List<string>();
                var invalidPlans = new List<string>();

                foreach (var planName in planNames)
                {
                    if (!BambooValidator.IsValidProjectPlanName(planName, out string message))
                    {
                        invalidPlans.Add(planName);
                        _logger.LogWarning(message);
                        continue;
                    }

                    var normalizedPlanName = BambooValidator.TrimToProjectPlanName(planName);

                    if (await _planChatRepository.AddPlanChatAsync(normalizedPlanName, chatId))
                    {
                        addedPlans.Add(normalizedPlanName);
                    }
                    else
                    {
                        existingPlans.Add(normalizedPlanName);
                    }
                }

                var response = CreateSubscriptionResponse(addedPlans, existingPlans, invalidPlans);
                await SendResponse(chatId, response.ToString(), kafkaMessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке команды подписки");
                await SendResponse(chatId, "Произошла ошибка при обработке команды\\.", kafkaMessageId);
            }
        }

        /// <summary>
        /// Обрабатывает команду отписки от уведомлений о неудачных сборках
        /// </summary>
        /// <param name="chatId">ID чата в Telegram</param>
        /// <param name="parameters">Параметры команды</param>
        /// <param name="kafkaMessageId">ID сообщения Kafka</param>
        public async Task ProcessUnsubscriptionCommand(string chatId, string parameters, string kafkaMessageId)
        {
            try
            {
                if (parameters == "")
                {
                    await SendUnsubscribeHelpMessage(chatId, kafkaMessageId);
                    return;
                }

                if (parameters == "all")
                {
                    if (await _planChatRepository.DeleteAllPlansFromChatAsync(chatId))
                    {
                        await SendResponse(chatId, "Вы успешно отписаны от всех планов сборок\\.", kafkaMessageId);
                        return;
                    }
                }

                var planNames = ParsePlanParameters(parameters);

                if (planNames.Count == 0)
                {
                    await SendResponse(chatId, "Не указаны планы сборки для отписки\\. Формат: Проект \\- План", kafkaMessageId);
                    return;
                }

                var removedPlans = new List<string>();
                var notSubscribedPlans = new List<string>();
                var invalidPlans = new List<string>();

                foreach (var planName in planNames)
                {
                    if (!BambooValidator.IsValidProjectPlanName(planName, out string message))
                    {
                        invalidPlans.Add(planName);
                        _logger.LogWarning(message);
                        continue;
                    }

                    var normalizedPlanName = BambooValidator.TrimToProjectPlanName(planName);

                    if (await _planChatRepository.DeletePlanChatAsync(normalizedPlanName, chatId))
                    {
                        removedPlans.Add(normalizedPlanName);
                    }
                    else
                    {
                        notSubscribedPlans.Add(normalizedPlanName);
                    }
                }

                var response = CreateUnsubscriptionResponse(removedPlans, notSubscribedPlans, invalidPlans);
                await SendResponse(chatId, response, kafkaMessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке команды отписки");
                await SendResponse(chatId, "Произошла ошибка при обработке команды\\. Попробуйте позже\\.", kafkaMessageId);
            }
        }

        /// <summary>
        /// Обрабатывает команду вывода списка текущих подписок
        /// </summary>
        /// <param name="chatId">ID чата в Telegram</param>
        /// <param name="kafkaMessageId">ID сообщения Kafka</param>
        public async Task ProcessListSubscriptionsCommand(string chatId, string kafkaMessageId)
        {
            try
            {
                var subscriptions = await _planChatRepository.GetPlanNamesAsync(chatId);

                if (subscriptions.Count == 0)
                {
                    await SendResponse(chatId, "У вас нет активных подписок на уведомления о сборках\\.", kafkaMessageId);
                    return;
                }

                var response = new StringBuilder();
                response.AppendLine("📋 Ваши текущие подписки на уведомления:");
                response.Append("**");
                response.AppendLine(string.Join("\n", subscriptions.Select((p, i) => $">{i + 1}\\. {TelegramMarkdownHelper.EscapeMarkdownV2(p)}")));
                response.AppendLine(">||");
                response.AppendLine("Для отписки используйте \\/unsubfailedbuildnotifier");

                await SendResponse(chatId, response.ToString(), kafkaMessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка подписок");
                await SendResponse(chatId, "Произошла ошибка при получении списка подписок\\. Попробуйте позже\\.", kafkaMessageId);
            }
        }

        private string CreateSubscriptionResponse(List<string> addedPlans, List<string> existingPlans, List<string> invalidPlans)
        {
            var response = new StringBuilder();

            if (addedPlans.Count != 0)
            {
                response.AppendLine("✅ Вы успешно подписаны на уведомления для сборок:");
                var text = string.Join("\n", addedPlans.Select(p => $"▪ {p}"));
                response.AppendLine(TelegramMarkdownHelper.EscapeMarkdownV2(text));
            }

            if (existingPlans.Count != 0)
            {
                if (response.Length > 0) response.AppendLine();
                response.AppendLine("ℹ️ Вы уже подписаны на эти сборки:");
                var text = string.Join("\n", existingPlans.Select(p => $"▪ {p}"));
                response.AppendLine(TelegramMarkdownHelper.EscapeMarkdownV2(text));
            }

            if (invalidPlans.Count != 0)
            {
                if (response.Length > 0) response.AppendLine();
                response.AppendLine("❌ Некорректные названия сборок \\(формат: Project \\- Plan\\):");
                var text = string.Join("\n", invalidPlans.Select(p => $"▪ {p}"));
                response.AppendLine(TelegramMarkdownHelper.EscapeMarkdownV2(text));
            }

            return response.ToString();
        }

        private string CreateUnsubscriptionResponse(List<string> removedPlans, List<string> notSubscribedPlans, List<string> invalidPlans)
        {
            var response = new StringBuilder();

            if (removedPlans.Count != 0)
            {
                response.AppendLine("✅ Вы успешно отписались от уведомлений для сборок:");
                var text = string.Join("\n", removedPlans.Select(p => $"▪ {p}"));
                response.AppendLine(TelegramMarkdownHelper.EscapeMarkdownV2(text));
            }

            if (notSubscribedPlans.Count != 0)
            {
                if (response.Length > 0) response.AppendLine();
                response.AppendLine("ℹ️ Вы не были подписаны на эти сборки:");
                var text = string.Join("\n", notSubscribedPlans.Select(p => $"▪ {p}"));
                response.AppendLine(TelegramMarkdownHelper.EscapeMarkdownV2(text));
            }

            if (invalidPlans.Count != 0)
            {
                if (response.Length > 0) response.AppendLine();
                response.AppendLine("❌ Некорректные названия сборок \\(формат: Проект \\- План\\):");
                var text = string.Join("\n", invalidPlans.Select(p => $"▪ {p}"));
                response.AppendLine(TelegramMarkdownHelper.EscapeMarkdownV2(text));
            }

            if (removedPlans.Count == 0 && notSubscribedPlans.Count > 0 && invalidPlans.Count == 0)
            {
                response.Insert(0, "ℹ️ ");
            }

            return response.ToString();
        }

        private async Task SendSubscribeHelpMessage(string chatId, string kafkaMessageId)
        {
            var helpMessage =
                """
                ℹ️ *Как подписаться на уведомления о неудачных сборках:*
                Используйте команду:
                ```
                \/subfailedbuildnotifier Project1 \- Plan1
                ```
                Можно указывать несколько планов через запятую или точку с запятой\.

                Формат названия плана сборки: \"[Project \- Plan](https://drive.google.com/file/d/1uZR6zDXhMe19hd1Y0OfhoxPExIiiGQyg/view?usp=sharing)\" \([?](https://confluence.atlassian.com/bamboo/bamboo-variables-289277087.html#:~:text=Some%20job%20name-,bamboo.planName,-The%20current%20plan%27s)\)
                """;

            await SendResponse(chatId, helpMessage, kafkaMessageId);
        }

        private async Task SendUnsubscribeHelpMessage(string chatId, string kafkaMessageId)
        {
            var helpMessage = """
                ℹ️ *Как отписаться от уведомлений о неудачных сборках:*
                Используйте команду:
                ```
                \/unsubfailedbuildnotifier Project1 \- Plan1
                ```
                Можно указывать несколько планов через запятую или точку с запятой\.

                Чтобы отписаться от всех планов, используйте команду:
                `\/unsubfailedbuildnotifier all`\.

                Чтобы посмотреть все свои подписки\, используйте 
                \/myfailedbuildnotifiersubs
                """;

            await SendResponse(chatId, helpMessage, kafkaMessageId);
        }

        private List<string> ParsePlanParameters(string parameters)
        {
            return [.. parameters.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                             .Select(p => p.Trim())
                             .Where(p => !string.IsNullOrWhiteSpace(p))];
        }

        private async Task SendResponse(string chatId, string messageText, string kafkaMessageId)
        {
            var botMessage = new BotMessage
            {
                Method = "sendMessage",
                KafkaMessageId = kafkaMessageId,
                Status = "COMPLETED",
                Data = new()
                {
                    Text = messageText,
                    ChatId = chatId,
                    Method = "sendMessage",
                    ParseMode = "MarkdownV2"
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(botMessage, JsonSettings.DefaultOptions);
            await _messageProducer.SendRequest(json);
        }
    }
}