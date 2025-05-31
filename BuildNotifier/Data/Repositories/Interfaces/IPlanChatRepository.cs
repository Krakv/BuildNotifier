using BuildNotifier.Data.Models.DB;

namespace BuildNotifier.Data.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий для работы с подписками чатов на планы сборок
    /// </summary>
    public interface IPlanChatRepository
    {

        /// <summary>
        /// Добавляет подписку чата на план сборки
        /// </summary>
        /// <param name="planName">Название плана сборки</param>
        /// <param name="chatId">Идентификатор чата</param>
        /// <returns>
        /// true - если подписка добавлена,
        /// false - если связь уже существует
        /// </returns>
        public bool AddPlanChat(string planName, string chatId);

        /// <summary>
        /// Удаляет конкретную подписку чата на план сборки
        /// </summary>
        /// <param name="planName">Название плана сборки</param>
        /// <param name="chatId">Идентификатор чата</param>
        /// <returns>
        /// true - если подписка удалена,
        /// false - если подписка не найдена
        /// </returns>
        public bool DeletePlanChat(string planName, string chatId);

        /// <summary>
        /// Удаляет все подписки для указанного чата
        /// </summary>
        /// <param name="chatId">Идентификатор чата</param>
        /// <returns>
        /// true - если подписки удалены,
        /// false - если подписок не найдено
        /// </returns>
        public bool DeleteAllPlansFromChat(string chatId);

        /// <summary>
        /// Получает список идентификаторов чатов, подписанных на указанный план
        /// </summary>
        /// <param name="planName">Название плана сборки</param>
        /// <returns>Список идентификаторов чатов</returns>
        public List<string> GetChatIds(string planName);

        /// <summary>
        /// Получает список названий планов, на которые подписан указанный чат
        /// </summary>
        /// <param name="chatId">Идентификатор чата</param>
        /// <returns>Список названий планов</returns>
        public List<string> GetPlanNames(string chatId);
    }
}
