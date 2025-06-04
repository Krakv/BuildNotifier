using BuildNotifier.Data.Models.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildNotifier.Services.Delegates
{
    /// <summary>
    /// Содержит кастомные делегаты
    /// </summary>
    public class CustomDelegates
    {
        /// <summary>
        /// Делегат для отправки сообщений
        /// </summary>
        /// <param name="text">Текст сообщения</param>
        /// <param name="chatId">Идентификатор чата</param>
        /// <param name="status">Статус сообщения (по умолчанию "COMPLETED")</param>
        /// <param name="inlineKeyboardMarkup">Опциональная inline-клавиатура (может быть null)</param>
        /// <param name="parseMode">Опциональный режим парсинга (например, "Markdown")</param>
        /// <param name="kafkaMessageId">Опциональный идентификатор сообщения Kafka (может быть null)</param>
        public delegate void SendMessageDelegate(
            string text,
            string chatId,
            string status = "COMPLETED",
            InlineKeyboardMarkup? inlineKeyboardMarkup = null,
            string? parseMode = null,
            string? kafkaMessageId = null);
    }
}
