using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.Bot
{
    /// <summary>
    /// Класс, представляющий inline-клавиатуру для сообщения.
    /// </summary>
    /// <remarks>
    /// Используется для создания кнопок под сообщением.
    /// </remarks>
    public class InlineKeyboardMarkup
    {
        /// <summary>
        /// Получает или задает список рядов кнопок клавиатуры.
        /// </summary>
        /// <value>
        /// Список списков кнопок <see cref="InlineKeyboardButton"/>. 
        /// Каждый внутренний список представляет собой ряд кнопок.
        /// По умолчанию - пустая коллекция.
        /// </value>
        [JsonPropertyName("inline_keyboard")]
        public List<List<InlineKeyboardButton>> InlineKeyboardButtons { get; set; } = new();
    }
}
