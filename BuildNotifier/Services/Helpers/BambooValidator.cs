using System.Text.RegularExpressions;

namespace BuildNotifier.Services.Helpers
{
    /// <summary>
    /// Предоставляет методы для валидации и обработки названия плана сборки Bamboo
    /// </summary>
    public class BambooValidator
    {
        /// <summary>
        /// Проверяет, что строка имеет формат "ПРОЕКТ - ПЛАН" (Допустимы пробелы, нижние подчеркивания и точки в названиях плана и проекта)
        /// Например: "Project - Plan", "App - Main"
        /// </summary>
        /// <param name="planName">Название плана сборки Bamboo (buildPlanName)</param>
        /// <returns>Возвращает результат проверки на соответствие формату</returns>
        public static bool IsValidProjectPlanName(string planName)
        {
            if (string.IsNullOrWhiteSpace(planName))
            {
                return false;
            }

            return Regex.IsMatch(planName, @"^[A-Za-z0-9.\s_]+\s-\s[A-Za-z0-9.\s_]+$");
        }

        /// <summary>
        /// Оставляет только первые две части названия плана и форматирует с пробелами (PROJ - PLAN - 123 → PROJ - PLAN, AD - MAIN - WEB → AD - MAIN)
        /// </summary>
        /// <param name="fullPlanName">Полное название плана сборки Bamboo</param>
        /// <returns>Название проекта и плана, разделенные дефисом с пробелами</returns>
        public static string TrimToProjectPlanName(string fullPlanName)
        {
            if (string.IsNullOrWhiteSpace(fullPlanName))
            {
                return fullPlanName;
            }

            var parts = fullPlanName.Split('-');
            if (parts.Length >= 2)
            {
                return $"{parts[0].Trim()} - {parts[1].Trim()}";
            }

            return fullPlanName;
        }

        /// <summary>
        /// Удаляет часть строки, содержащую email в угловых скобках, оставляя только имя/логин.
        /// </summary>
        /// <param name="input">Входная строка, возможно содержащая email</param>
        /// <returns>Строка без email-части</returns>
        public static string RemoveEmail(string input)
        {
            return Regex.Replace(input, @"\s*<.*?>", "").Trim();
        }
    }
}
