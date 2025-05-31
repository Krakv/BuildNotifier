using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.Bot
{
    public class MessageData
    {
        [JsonPropertyName("chat_id")]
        public string ChatId { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("parse_mode")]
        public string? ParseMode { get; set; }

        [JsonPropertyName("reply_markup")]
        public InlineKeyboardMarkup? ReplyMarkup { get; set; }
    }
}
