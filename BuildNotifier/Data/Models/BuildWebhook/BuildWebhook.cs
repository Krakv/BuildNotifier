using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.BambooWebhookPayload
{
    /// <summary>
    /// Класс, представляющий вебхук-событие сборки.
    /// </summary>
    /// <remarks>
    /// Содержит полную информацию о событии сборки в системе CI/CD.
    /// Все свойства обязательны для заполнения (required).
    /// </remarks>
    public class BuildWebhook
    {
        /// <summary>
        /// Получает или задает название сервиса, которое получает хук.
        /// </summary>
        /// <value>
        /// Строка с названием сервиса. Обязательное поле.
        /// </value>
        [JsonPropertyName("serviceName")]
        public required string ServiceName { get; set; }

        /// <summary>
        /// Получает или задает уникальный идентификатор события.
        /// </summary>
        /// <value>
        /// UUID в строковом формате. Обязательное поле.
        /// </value>
        [JsonPropertyName("uuid")]
        public required string Uuid { get; set; }

        /// <summary>
        /// Получает или задает URL репозитория с исходным кодом.
        /// </summary>
        /// <value>
        /// Строка с URL Git-репозитория. Обязательное поле.
        /// </value>
        [JsonPropertyName("repositoryUrl")]
        public required string RepositoryUrl { get; set; }

        /// <summary>
        /// Получает или задает название ветки, для которой выполнена сборка.
        /// </summary>
        /// <value>
        /// Строка с названием ветки. Обязательное поле.
        /// </value>
        [JsonPropertyName("branchName")]
        public required string BranchName { get; set; }

        /// <summary>
        /// Получает или задает информацию о коммите.
        /// </summary>
        /// <value>
        /// Объект <see cref="CommitInfo"/> с деталями коммита. Обязательное поле.
        /// </value>
        [JsonPropertyName("commit")]
        public required CommitInfo Commit { get; set; }

        /// <summary>
        /// Получает или задает информацию о сборке.
        /// </summary>
        /// <value>
        /// Объект <see cref="BuildInfo"/> с деталями сборки. Обязательное поле.
        /// </value>
        [JsonPropertyName("build")]
        public required BuildInfo Build { get; set; }

        /// <summary>
        /// Получает или задает временную метку события.
        /// </summary>
        /// <value>
        /// Строка с временем события. Обязательное поле.
        /// </value>
        [JsonPropertyName("time")]
        public required string Time { get; set; }
    }

}
