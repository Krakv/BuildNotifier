using Microsoft.Extensions.Logging;
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
        public static bool IsValidProjectPlanName(string planName, out string message)
        {
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(planName))
            {
                message = "Название плана не может быть пустым или содержать только пробелы";
                return false;
            }

            if (!Regex.IsMatch(planName, @"^.+\s-\s.+$"))
            {
                message = $"Название плана '{planName}' не содержит разделителя ' - '";
                return false;
            }

            var parts = planName.Split(new[] { " - " }, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                message = $"Название плана '{planName}' должно содержать ровно одну пару ' - '";
                return false;
            }

            var projectName = parts[0];
            var planPart = parts[1];

            if (!Regex.IsMatch(projectName, @"^[A-Za-z0-9._\s]+$"))
            {
                message = $"Название проекта '{projectName}' содержит недопустимые символы";
                return false;
            }

            if (!Regex.IsMatch(planPart, @"^[A-Za-z0-9._\s]+$"))
            {
                message = $"Название плана '{planPart}' содержит недопустимые символы";
                return false;
            }

            return true;
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
        /// Извлекает первую часть email (до символа '@') из строки формата "Имя <email@domain.com>"
        /// </summary>
        /// <param name="input">Входная строка, например: "Иван Петров <ivan@gmail.com>"</param>
        /// <returns>Первая часть email или пустая строка, если email не найден</returns>
        public static string GetEmailFirstPart(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            Match match = Regex.Match(input, @"<([^@]+)");

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }
    }
}
