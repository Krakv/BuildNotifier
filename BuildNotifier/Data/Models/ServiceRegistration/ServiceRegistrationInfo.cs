using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.ServiceRegistration
{
    /// <summary>
    /// Представляет информацию о регистрации сервиса в системе.
    /// </summary>
    public class ServiceRegistrationInfo
    {
        /// <summary>
        /// Получает или задает название сервиса.
        /// </summary>
        /// <value>Название сервиса в виде строки.</value>
        [JsonPropertyName("serviceName")]
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Получает или задает топик, который сервис потребляет (подписывается на сообщения).
        /// </summary>
        /// <value>Имя топика для потребления сообщений.</value>
        [JsonPropertyName("consumeTopic")]
        public string ConsumeTopic { get; set; } = string.Empty;

        /// <summary>
        /// Получает или задает топик, в который сервис публикует сообщения.
        /// </summary>
        /// <value>Имя топика для публикации сообщений.</value>
        [JsonPropertyName("produceTopic")]
        public string ProduceTopic { get; set; } = string.Empty;

        /// <summary>
        /// Получает или задает сервисное сообщение.
        /// </summary>
        /// <value>Дополнительная информация в виде строки.</value>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
