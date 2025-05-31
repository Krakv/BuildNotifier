using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.BambooWebhookPayload
{
    public class BuildWebhook
    {
        [JsonPropertyName("uuid")]
        public required string Uuid { get; set; }

        [JsonPropertyName("repositoryUrl")]
        public required string RepositoryUrl { get; set; }

        [JsonPropertyName("branchName")]
        public required string BranchName { get; set; }

        [JsonPropertyName("commit")]
        public required CommitInfo Commit { get; set; }

        [JsonPropertyName("build")]
        public required BuildInfo Build { get; set; }

        [JsonPropertyName("time")]
        public required string Time { get; set; }
    }
    
}
