using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.Kafka
{
    /// <summary>
    /// Конфигурация, содержащая топик Kafka для прослушивания входящих вебхуков.
    /// </summary>
    public class KafkaTopics
    {
        /// <summary>
        /// Получает или задает топик для прослушивания входящих вебхуков.
        /// </summary>
        /// <value>
        /// Топик Kafka в виде строки.
        /// По умолчанию принимает значение пустой строки.
        /// </value>
        public string WebhookTopic { get; set; } = string.Empty;
    }
}
