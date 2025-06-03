namespace BuildNotifier.Data.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий для работы с подписками чатов на планы сборок
    /// </summary>
    public interface IPlanChatRepository
    {
        /// <summary>
        /// Добавляет подписку чата на план сборки (асинхронно)
        /// </summary>
        /// <param name="planName">Название плана сборки</param>
        /// <param name="chatId">Идентификатор чата</param>
        /// <returns>
        /// Task<bool>, где:
        /// true - если подписка добавлена,
        /// false - если связь уже существует
        /// </returns>
        public Task<bool> AddPlanChatAsync(string planName, string chatId);

        /// <summary>
        /// Удаляет конкретную подписку чата на план сборки (асинхронно)
        /// </summary>
        /// <param name="planName">Название плана сборки</param>
        /// <param name="chatId">Идентификатор чата</param>
        /// <returns>
        /// Task<bool>, где:
        /// true - если подписка удалена,
        /// false - если подписка не найдена
        /// </returns>
        public Task<bool> DeletePlanChatAsync(string planName, string chatId);

        /// <summary>
        /// Удаляет все подписки для указанного чата (асинхронно)
        /// </summary>
        /// <param name="chatId">Идентификатор чата</param>
        /// <returns>
        /// Task<bool>, где:
        /// true - если подписки удалены,
        /// false - если подписок не найдено
        /// </returns>
        public Task<bool> DeleteAllPlansFromChatAsync(string chatId);

        /// <summary>
        /// Получает список идентификаторов чатов, подписанных на указанный план (асинхронно)
        /// </summary>
        /// <param name="planName">Название плана сборки</param>
        /// <returns>
        /// Task<List<string>> - список идентификаторов чатов
        /// </returns>
        public Task<List<string>> GetChatIdsAsync(string planName);

        /// <summary>
        /// Получает список названий планов, на которые подписан указанный чат (асинхронно)
        /// </summary>
        /// <param name="chatId">Идентификатор чата</param>
        /// <returns>
        /// Task<List<string>> - список названий планов
        /// </returns>
        public Task<List<string>> GetPlanNamesAsync(string chatId);
    }
}