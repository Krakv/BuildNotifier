using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.BambooWebhookPayload
{
    public class CommitInfo
    {
        [JsonPropertyName("hash")]
        public required string Hash { get; set; }

        [JsonPropertyName("author")]
        public required string Author { get; set; }

        [JsonPropertyName("message")]
        public required string Message { get; set; }
    }

}
