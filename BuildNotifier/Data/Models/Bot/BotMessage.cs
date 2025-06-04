using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.Bot
{
    /// <summary>
    /// Класс, представляющий сообщение бота для обмена данными.
    /// </summary>
    /// <remarks>
    /// Используется для сериализации/десериализации сообщений между компонентами бота.
    /// Имена свойств в JSON явно заданы с помощью атрибутов JsonPropertyName.
    /// </remarks>
    public class BotMessage
    {
        /// <summary>
        /// Получает или задает название метода/действия для выполнения.
        /// </summary>
        /// <value>
        /// Строка с названием метода (Пример: "sendmessage").
        /// По умолчанию - пустая строка.
        /// </value>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Получает или задает имя файла, связанного с сообщением (если есть).
        /// </summary>
        /// <value>
        /// Строка с именем файла или null, если файл не прилагается.
        /// </value>
        public string? Filename { get; set; }

        /// <summary>
        /// Получает или задает основные данные сообщения.
        /// </summary>
        /// <value>
        /// Объект <see cref="MessageData"/> с содержимым сообщения.
        /// Обязательное поле (вызывает NullReferenceException, если не инициализировано).
        /// </value>
        public MessageData Data { get; set; } = null!;

        /// <summary>
        /// Получает или задает уникальный идентификатор сообщения в Kafka.
        /// </summary>
        /// <value>
        /// Строка с ID сообщения Kafka.
        /// По умолчанию - пустая строка.
        /// </value>
        public string KafkaMessageId { get; set; } = string.Empty;

        /// <summary>
        /// Получает или задает текущий статус обработки сообщения.
        /// </summary>
        /// <value>
        /// Строка со статусом (COMPLETED, IN_PROGRESS),
        /// или null, если статус не применим.
        /// </value>
        public string? Status { get; set; }
    }
}
