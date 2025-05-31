using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.Bot
{
    public class BotMessage
    {
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("filename")]
        public string? Filename { get; set; }

        [JsonPropertyName("data")]
        public MessageData Data { get; set; } = null!;

        [JsonPropertyName("kafkaMessageId")]
        public string KafkaMessageId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}
