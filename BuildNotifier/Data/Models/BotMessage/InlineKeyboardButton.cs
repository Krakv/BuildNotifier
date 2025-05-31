using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.Bot
{
    public class InlineKeyboardButton
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("callback_data")]
        public string? CallbackData { get; set; }
    }
}
