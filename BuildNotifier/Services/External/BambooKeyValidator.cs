using System.Text.RegularExpressions;

namespace BuildNotifier.Services.External
{
    /// <summary>
    /// Предоставляет методы для валидации и обрезания части ключа сборки Bamboo
    /// </summary>
    public class BambooKeyValidator
    {
        /// <summary>
        /// Проверяет, что строка имеет формат "PROJECT-PLAN" (только буквы/цифры и один дефис)
        /// </summary>
        /// <param name="key">Ключ сборки Bamboo (buildResultKey)</param>
        /// <returns>Возвращает результат проверки на соответствие формату</returns>
        public static bool IsValidProjectPlanKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return Regex.IsMatch(key, @"^[A-Za-z0-9]+-[A-Za-z0-9]+$");
        }

        /// <summary>
        /// Оставляет только первые две части кода сборки (PROJ-PLAN-123 → PROJ-PLAN, AD-MAIN-WEB-79 → AD-MAIN)
        /// </summary>
        /// <param name="buildResultKey">Ключ сборки Bamboo (buildResultKey)</param>
        /// <returns>Первые две части кода сборки, разделенные дефисом</returns>
        public static string TrimBuildNumber(string buildResultKey)
        {
            if (string.IsNullOrWhiteSpace(buildResultKey))
            {
                return buildResultKey;
            }

            var parts = buildResultKey.Split('-');
            if (parts.Length >= 2)
            {
                return $"{parts[0]}-{parts[1]}";
            }

            return buildResultKey;
        }
    }
}
