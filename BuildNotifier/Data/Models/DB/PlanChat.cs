namespace BuildNotifier.Data.Models.DB
{
    /// <summary>
    /// Класс представляет таблицу в базе данных для связи названия плана сборки и id чата в телеграме.
    /// </summary>
    public class PlanChat
    {
        /// <summary>
        /// Получает и задает название плана сборки.
        /// </summary>
        /// <value>
        /// Строка - название плана сборки.
        /// Обязательное поле (вызывает NullReferenceException, если не инициализировано).
        /// </value>
        public string PlanName { get; set; } = null!;
        /// <summary>
        /// Получает и задает id чата в телеграме.
        /// </summary>
        /// <value>
        /// Строка - id чата в телеграме.
        /// Обязательное поле (вызывает NullReferenceException, если не инициализировано).
        /// </value>
        public string ChatId { get; set; } = null!;

    }
}
