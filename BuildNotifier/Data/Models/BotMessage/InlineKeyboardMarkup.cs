using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.Bot
{
    public class InlineKeyboardMarkup
    {
        [JsonPropertyName("inline_keyboard")]
        public List<List<InlineKeyboardButton>> inlineKeyboardButtons { get; set; } = new();
    }
}
