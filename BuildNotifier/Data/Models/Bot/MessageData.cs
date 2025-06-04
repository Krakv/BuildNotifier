using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.Bot
{
    /// <summary>
    /// Класс, содержащий основные данные сообщения для бота.
    /// </summary>
    /// <remarks>
    /// Содержит информацию, необходимую для отправки или обработки сообщения в чате.
    /// </remarks>
    public class MessageData
    {
        /// <summary>
        /// Получает или задает идентификатор чата.
        /// </summary>
        /// <value>
        /// Строка с уникальным ID чата. По умолчанию - пустая строка.
        /// </value>
        [JsonPropertyName("chat_id")]
        public string ChatId { get; set; } = string.Empty;

        /// <summary>
        /// Получает или задает текст сообщения.
        /// </summary>
        /// <value>
        /// Строка с текстом сообщения. По умолчанию - пустая строка.
        /// </value>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Получает или задает метод обработки сообщения.
        /// </summary>
        /// <value>
        /// Строка с названием метода (Пример: "sendMessage").
        /// По умолчанию - пустая строка.
        /// </value>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Получает или задает режим парсинга текста сообщения.
        /// </summary>
        /// <value>
        /// Строка с режимом парсинга (например, "HTML", "MarkdownV2") или null, если парсинг не требуется.
        /// </value>
        [JsonPropertyName("parse_mode")]
        public string? ParseMode { get; set; }

        /// <summary>
        /// Получает или задает разметку клавиатуры для сообщения.
        /// </summary>
        /// <value>
        /// Объект <see cref="InlineKeyboardMarkup"/> с разметкой клавиатуры или null, если клавиатура не требуется.
        /// </value>
        [JsonPropertyName("reply_markup")]
        public InlineKeyboardMarkup? ReplyMarkup { get; set; }
    }
}
