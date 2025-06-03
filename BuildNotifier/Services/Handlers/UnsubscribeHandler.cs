using BuildNotifier.Data.Models.Bot;
using BuildNotifier.Data.Repositories;
using BuildNotifier.Data.Models.Pagination;
using BuildNotifier.Services.Delegates;

namespace BuildNotifier.Services.Handlers
{
    /// <summary>
    /// Обработчик для управления отпиской от уведомлений о сборках Bamboo
    /// </summary>
    public class UnsubscribeHandler
    {
        private readonly PlanChatRepository _planChatRepository;
        private readonly ILogger _logger;
        private readonly CustomDelegates.SendMessageDelegate _sendMessage;
        private readonly Func<Task<BotMessage?>> _getAnswer;
        private readonly Action<string> _onSessionEnded;

        /// <summary>
        /// Инициализирует новый экземпляр обработчика отписки
        /// </summary>
        /// <param name="planChatRepository">Репозиторий для работы с подписками</param>
        /// <param name="logger">Логгер для записи событий</param>
        /// <param name="sendMessage">Делегат для отправки сообщений</param>
        /// <param name="getAnswer">Функция получения ответа пользователя</param>
        /// <param name="onSessionEnded">Действие при завершении сессии</param>
        public UnsubscribeHandler(
            PlanChatRepository planChatRepository,
            ILogger logger,
            CustomDelegates.SendMessageDelegate sendMessage,
            Func<Task<BotMessage?>> getAnswer,
            Action<string> onSessionEnded)
        {
            _planChatRepository = planChatRepository;
            _logger = logger;
            _sendMessage = sendMessage;
            _getAnswer = getAnswer;
            _onSessionEnded = onSessionEnded;
        }

        /// <summary>
        /// Обрабатывает процесс отписки от уведомлений о сборках
        /// </summary>
        /// <param name="initialMessage">Сообщение, инициировавшее процесс отписки</param>
        /// <param name="cancellationToken">Токен для отмены операции</param>
        /// <returns>Task, представляющий асинхронную операцию</returns>
        public async Task HandleUnsubscribe(BotMessage initialMessage, CancellationToken cancellationToken)
        {
            try
            {
                var allPlanNames = await _planChatRepository.GetPlanNamesAsync(initialMessage.Data.ChatId);
                if (!allPlanNames.Any())
                {
                    _sendMessage("У вас нет активных подписок.", 
                        initialMessage.Data.ChatId, 
                        kafkaMessageId: initialMessage.KafkaMessageId);
                    return;
                }

                var state = new PaginationState
                {
                    AllPlanNames = allPlanNames,
                    CurrentPage = 0,
                    PageSize = 5,
                    TotalPages = (int)Math.Ceiling(allPlanNames.Count / 5.0)
                };

                ShowPage(state, initialMessage);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = await _getAnswer();
                    if (message == null) continue;

                    switch (message.Data.Text)
                    {
                        case "failed_build_notifier_previous_page":
                            if (state.CurrentPage > 0)
                            {
                                state.CurrentPage--;
                                ShowPage(state, message);
                            }
                            break;

                        case "failed_build_notifier_next_page":
                            if (state.CurrentPage < state.TotalPages - 1)
                            {
                                state.CurrentPage++;
                                ShowPage(state, message);
                            }
                            break;

                        case "failed_build_notifier_unsub_all":
                            HandleUnsubscribeAll(message);
                            return;

                        case "close_failed_build_notifier_session":
                            HandleSessionClose(message);
                            return;

                        default:
                            HandlePlanUnsubscribe(message, state, cancellationToken);
                            break;
                    }
                }
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Таймаут сессии отписки");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Операция отменена.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отписке");
                _sendMessage("Что-то пошло не так.", initialMessage.Data.ChatId,
                    kafkaMessageId: initialMessage.KafkaMessageId);
            }
            finally
            {
                _onSessionEnded(initialMessage.Data.ChatId);
            }
        }

        private async void HandleUnsubscribeAll(BotMessage message)
        {
            if (await _planChatRepository.DeleteAllPlansFromChatAsync(message.Data.ChatId))
            {
                _sendMessage("Вы успешно отписались от всех проектов.",
                    message.Data.ChatId,
                    status: "COMPLETED",
                    kafkaMessageId: message.KafkaMessageId);
            }
            else
            {
                _sendMessage("Проекты не найдены.",
                    message.Data.ChatId,
                    status: "IN_PROGRESS",
                    kafkaMessageId: message.KafkaMessageId);
            }
        }

        private void HandleSessionClose(BotMessage message)
        {
            _sendMessage("Сессия отписки завершена.",
                message.Data.ChatId,
                status: "COMPLETED",
                kafkaMessageId: message.KafkaMessageId);
        }

        private async void HandlePlanUnsubscribe(BotMessage message, PaginationState state, CancellationToken cancellationToken)
        {
            if (!message.Data.Text.StartsWith("failed_build_notifier_option_")) return;

            var optionKey = message.Data.Text.Last();
            if (!state.CurrentPagePlans.TryGetValue(optionKey, out var planName)) return;

            if (!await _planChatRepository.DeletePlanChatAsync(planName, message.Data.ChatId))
            {
                _sendMessage($"Проект под названием \"{planName}\" не найден.",
                    message.Data.ChatId,
                    status: "IN_PROGRESS",
                    parseMode: "MarkdownV2",
                    kafkaMessageId: message.KafkaMessageId);
                return;
            }

            state.AllPlanNames = await _planChatRepository.GetPlanNamesAsync(message.Data.ChatId);
            state.TotalPages = (int)Math.Ceiling(state.AllPlanNames.Count / (double)state.PageSize);

            if (state.CurrentPage >= state.TotalPages && state.TotalPages > 0)
                state.CurrentPage = state.TotalPages - 1;

            _sendMessage($"Вы успешно отписались от {planName}.",
                message.Data.ChatId,
                status: "IN_PROGRESS",
                kafkaMessageId: message.KafkaMessageId);

            if (!state.AllPlanNames.Any())
            {
                _sendMessage("У вас больше нет активных подписок.",
                    message.Data.ChatId,
                    status: "COMPLETED",
                    kafkaMessageId: message.KafkaMessageId);
                return;
            }

            ShowPage(state, message);
        }

        private void ShowPage(PaginationState state, BotMessage message)
        {
            var pagePlanNames = state.AllPlanNames
                .Skip(state.CurrentPage * state.PageSize)
                .Take(state.PageSize)
                .ToList();

            state.CurrentPagePlans.Clear();
            char optionKey = 'A';
            foreach (var planName in pagePlanNames)
            {
                state.CurrentPagePlans[optionKey++] = planName;
            }

            var instruction = "Выберите проект для отписки:";
            var pageInfo = $"Страница {state.CurrentPage + 1} из {state.TotalPages}";

            var buttons = new List<List<InlineKeyboardButton>>();

            optionKey = 'A';
            foreach (var planName in pagePlanNames)
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    new InlineKeyboardButton
                    {
                        Text = planName,
                        CallbackData = $"failed_build_notifier_option_{optionKey++}"
                    }
                });
            }

            if (state.TotalPages > 1)
            {
                var navButtons = new List<InlineKeyboardButton>();
                if (state.CurrentPage > 0)
                    navButtons.Add(new InlineKeyboardButton { Text = "← Назад", CallbackData = "failed_build_notifier_previous_page" });
                if (state.CurrentPage < state.TotalPages - 1)
                    navButtons.Add(new InlineKeyboardButton { Text = "Вперед →", CallbackData = "failed_build_notifier_next_page" });
                buttons.Add(navButtons);
            }

            buttons.Add(new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton { Text = "Отписаться от всех", CallbackData = "failed_build_notifier_unsub_all" },
                new InlineKeyboardButton { Text = "Завершить", CallbackData = "close_failed_build_notifier_session" }
            });

            _sendMessage($"{instruction}\n{pageInfo}",
                message.Data.ChatId,
                status: "IN_PROGRESS",
                parseMode: "MarkdownV2",
                kafkaMessageId: message.KafkaMessageId,
                inlineKeyboardMarkup: new InlineKeyboardMarkup { InlineKeyboardButtons = buttons });
        }
    }
}
