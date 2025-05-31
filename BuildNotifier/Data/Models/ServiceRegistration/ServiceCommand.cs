using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.ServiceRegistration
{
    /// <summary>
    /// Представляет команду, которую поддерживает сервис.
    /// </summary>
    public class ServiceCommand
    {
        /// <summary>
        /// Получает или задает имя команды.
        /// </summary>
        /// <value>Уникальное имя команды.</value>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Получает или задает описание команды.
        /// </summary>
        /// <value>Подробное описание функциональности команды.</value>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Получает или задает действие с командой.
        /// </summary>
        /// <value>Обозначает, что нужно делать с командой в командном менеджере (ADD, REMOVE, NONE).</value>
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Получает или задает права, необходимые для выполнения команды.
        /// </summary>
        /// <value>Строка с описанием требуемых прав (ANONYMOUS, GROUP, REGISTERED).</value>
        [JsonPropertyName("right")]
        public string Right { get; set; } = string.Empty;

        [JsonPropertyName("availability")]
        public string Availability {  get; set; } = string.Empty;
    }
}
