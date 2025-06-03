using BuildNotifier.Data.Models.BambooWebhookPayload;
using System.Text;

namespace BuildNotifier.Services.Helpers
{
    /// <summary>
    /// Вспомогательный класс для форматирования сообщения для отправки в телеграм
    /// </summary>
    public static class TelegramMarkdownHelper
    {
        private static readonly char[] _specialChars = new char[]
        {
        '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!', '\\', '<', '>'
        };

        /// <summary>
        /// Экранирует специальные символы MarkdownV2 в тексте
        /// </summary>
        /// <param name="text">Исходный текст для экранирования</param>
        /// <returns>
        /// Текст с экранированными специальными символами.
        /// Возвращает исходный текст если он null или пустой.
        /// </returns>
        public static string EscapeMarkdownV2(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var escapedText = new StringBuilder();
            foreach (char c in text)
            {
                if (Array.IndexOf(_specialChars, c) != -1)
                    escapedText.Append('\\');
                escapedText.Append(c);
            }
            return escapedText.ToString();
        }

        /// <summary>
        /// Форматирует текст как моноширинный (inline code) в MarkdownV2
        /// </summary>
        /// <param name="text">Текст для форматирования</param>
        /// <returns>
        /// Текст в моноширинном формате: `текст`.
        /// Возвращает исходный текст если он null или пустой.
        /// </returns>
        public static string GetMono(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return $"`{EscapeMarkdownV2(text)}`";
        }
    }
}
