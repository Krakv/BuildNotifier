using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.Bot
{
    /// <summary>
    /// Класс, представляющий кнопку inline-клавиатуры.
    /// </summary>
    public class InlineKeyboardButton
    {
        /// <summary>
        /// Получает или задает текст, отображаемый на кнопке.
        /// </summary>
        /// <value>
        /// Строка с текстом кнопки. По умолчанию - пустая строка.
        /// </value>
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Получает или задает URL, открываемый при нажатии на кнопку.
        /// </summary>
        /// <value>
        /// Строка с URL или null, если кнопка не ссылочная.
        /// </value>
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>
        /// Получает или задает данные для обратного вызова при нажатии на кнопку.
        /// </summary>
        /// <value>
        /// Строка с данными callback или null, если кнопка не поддерживает callback.
        /// </value>
        [JsonPropertyName("callback_data")]
        public string? CallbackData { get; set; }
    }
}
