using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.HTTPClient
{
    /// <summary>
    /// Представляет конфигурацию, содержащую URL для запроса к API
    /// </summary>
    public class TelegramNicknameService
    {
        /// <summary>
        /// Получает или задает URL для запроса к API
        /// </summary>
        /// <value>URL адрес в виде строки</value>
        [JsonPropertyName("apiUrl")]
        public required string ApiUrl { get; set; } = string.Empty;
    }
}
