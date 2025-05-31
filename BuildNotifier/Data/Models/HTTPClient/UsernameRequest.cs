using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.HTTPClient
{
    /// <summary>
    /// Представляет JSON тело запроса к API для получения username в телеграме по логину в доменной учетной записи
    /// </summary>
    public class UsernameRequest
    {
        /// <summary>
        /// Получает или задает логин в доменной учетной записи
        /// </summary>
        /// <value>Логин в доменной учетной записи.</value>
        [JsonPropertyName("username")]
        public required string Username { get; set; }
    }
}
