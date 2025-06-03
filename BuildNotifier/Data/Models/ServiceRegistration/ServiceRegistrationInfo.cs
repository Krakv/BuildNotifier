using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.ServiceRegistration
{
    /// <summary>
    /// Представляет информацию о регистрации сервиса в командном менеджере.
    /// </summary>
    public class ServiceRegistrationInfo
    {
        /// <summary>
        /// Задает название сервиса.
        /// </summary>
        /// <value>Название сервиса в виде строки.</value>
        [JsonPropertyName("serviceName")]
        public required string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Задает топик, который сервис потребляет (подписывается на сообщения).
        /// </summary>
        /// <value>Имя топика для потребления сообщений.</value>
        [JsonPropertyName("consumeTopic")]
        public required string ConsumeTopic { get; set; } = string.Empty;

        /// <summary>
        /// Задает топик, в который сервис публикует сообщения.
        /// </summary>
        /// <value>Имя топика для публикации сообщений.</value>
        [JsonPropertyName("produceTopic")]
        public required string ProduceTopic { get; set; } = string.Empty;

        /// <summary>
        /// Задает сервисное сообщение.
        /// </summary>
        /// <value>Дополнительная информация в виде строки.</value>
        [JsonPropertyName("message")]
        public required string Message { get; set; } = string.Empty;
    }
}
