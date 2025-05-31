using System.Text.Json;

namespace BuildNotifier.Services.External
{
    /// <summary>
    /// Класс для безопасного парсинга JSON-данных
    /// </summary>
    /// <remarks>
    /// Предоставляет методы для обработки JSON с защитой от исключений.
    /// Использует System.Text.Json для десериализации.
    /// </remarks>
    public class JsonParser
    {
        /// <summary>
        /// Пытается десериализовать JSON строку в указанный тип
        /// </summary>
        /// <typeparam name="T">Тип для десериализации</typeparam>
        /// <param name="json">JSON строка для парсинга</param>
        /// <param name="result">Результат десериализации (null если не удалось)</param>
        /// <returns>
        /// true - если десериализация прошла успешно и результат не null,
        /// false - если произошла ошибка парсинга или результат null
        /// </returns>
        public static bool TryParseJson<T>(string json, out T? result)
        {
            result = default;
            try
            {
                result = JsonSerializer.Deserialize<T>(json);
                return result != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
