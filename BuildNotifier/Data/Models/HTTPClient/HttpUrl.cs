using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.HTTPClient
{
    /// <summary>
    /// Представляет конфигурацию, содержащую URL для http запроса
    /// </summary>
    public class HttpUrl
    {
        /// <summary>
        /// Получает или задает URL для http запроса
        /// </summary>
        /// <value>URL адрес в виде строки</value>
        [JsonPropertyName("apiUrl")]
        public required string Url { get; set; }
    }
}
