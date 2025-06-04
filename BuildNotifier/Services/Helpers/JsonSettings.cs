using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BuildNotifier.Services.Helpers
{
    /// <summary>
    /// Предоставляет стандартные настройки сериализации JSON для приложения
    /// </summary>
    public static class JsonSettings
    {
        /// <summary>
        /// Возвращает предварительно настроенные параметры сериализации JSON
        /// </summary>
        /// <remarks>
        /// Настройки включают:
        /// - сamelCase для имен свойств
        /// - Форматированный вывод (с отступами)
        /// - Игнорирование null-значений
        /// - Минимальное экранирование символов
        /// </remarks>
        public static JsonSerializerOptions DefaultOptions => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

            WriteIndented = true,

            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
    }
}
