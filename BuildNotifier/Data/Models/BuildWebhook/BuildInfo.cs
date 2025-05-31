using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.BambooWebhookPayload
{
    public class BuildInfo
    {
        [JsonPropertyName("buildResultKey")]
        public required string BuildResultKey { get; set; }

        [JsonPropertyName("status")]
        public required string Status { get; set; }

        [JsonPropertyName("buildPlanName")]
        public required string BuildPlanName { get; set; }
    }
}
