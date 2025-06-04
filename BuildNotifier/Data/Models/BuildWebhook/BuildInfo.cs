using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.BambooWebhookPayload
{
    /// <summary>
    /// Класс, содержащий информацию о сборке.
    /// </summary>
    public class BuildInfo
    {
        /// <summary>
        /// Получает или задает уникальный ключ результата сборки.
        /// </summary>
        /// <value>
        /// Строка с идентификатором сборки. Обязательное поле.
        /// </value>
        public required string BuildResultKey { get; set; }

        /// <summary>
        /// Получает или задает статус сборки.
        /// </summary>
        /// <value>
        /// Строка со статусом (например, "SUCCESS", "FAILED"). Обязательное поле.
        /// </value>
        public required string Status { get; set; }

        /// <summary>
        /// Получает или задает название плана сборки.
        /// </summary>
        /// <value>
        /// Строка с названием конфигурации сборки. Обязательное поле.
        /// </value>
        public required string BuildPlanName { get; set; }
    }
}
