using BuildNotifier.Data.Models.HTTPClient;
using System.Net.Http.Json;

namespace BuildNotifier.Services.External
{
    /// <summary>
    /// Клиент для отправки запросов к API по http
    /// </summary>
    public class ApiHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiHttpClient> _logger;

        /// <summary>
        /// Клиент для отправки запросов к API по http
        /// </summary>
        /// <param name="httpClient">Клиент для отправки запросов по http</param>
        /// <param name="logger">Логгер для вывода информации о внутренних процессах</param>
        public ApiHttpClient(HttpClient httpClient, ILogger<ApiHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Запрашивает username в телеграме по логину в доменной учетной записи
        /// </summary>
        /// <param name="apiUrl">Адрес для запроса</param>
        /// <param name="username">Логин в доменной учетной записи</param>
        /// <returns>Username в телеграме</returns>
        public async Task<string> GetStringResponseAsync(string apiUrl, string username)
        {
            try
            {
                var request = new UsernameRequest { Username = username };

                var response = await _httpClient.PostAsJsonAsync(apiUrl, request);

                if ((int)response.StatusCode / 100 == 4 || (int)response.StatusCode / 100 == 5)
                {
                    _logger.LogWarning("API вернул статус {StatusCode} для пользователя {Username}", response.StatusCode, username);
                    return string.Empty;
                }

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении HTTP-запроса");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Неожиданная ошибка");
                throw;
            }
        }
    }
}
