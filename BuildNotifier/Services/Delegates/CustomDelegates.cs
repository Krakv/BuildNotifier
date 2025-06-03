using BuildNotifier.Data.Models.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildNotifier.Services.Delegates
{
    public class CustomDelegates
    {
        public delegate void SendMessageDelegate(
            string text,
            string chatId,
            string status = "COMPLETED",
            InlineKeyboardMarkup? inlineKeyboardMarkup = null,
            string? parseMode = null,
            string? kafkaMessageId = null);
    }
}
