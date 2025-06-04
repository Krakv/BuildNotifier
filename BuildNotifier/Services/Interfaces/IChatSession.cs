using BuildNotifier.Data.Models.Bot;
using BuildNotifier.Services.Delegates;
using System.Runtime.CompilerServices;

namespace BuildNotifier.Services.Interfaces
{

    /// <summary>
    /// Представляет сессию чата c обменом сообщениями между разными сервисами
    /// </summary>
    public interface IChatSession
    {
        /// <summary>
        /// Событие, возникающее при необходимости отправить сообщение
        /// </summary>
        event Action<string>? OnSendMessage;

        /// <summary>
        /// Событие, возникающее при инициировании завершения сессии изнутри
        /// </summary>
        event Action<string>? OnSessionEnded;

        /// <summary>
        /// Идентификатор чата, связанного с сессией
        /// </summary>
        string ChatId { get; }

        /// <summary>
        /// Асинхронно обрабатывает входящее сообщение
        /// </summary>
        /// <param name="botMessage">Входящее сообщение от бота</param>
        Task ProcessMessageAsync(BotMessage botMessage);

        /// <summary>
        /// Запускает сессию с указанным начальным сообщением
        /// </summary>
        /// <param name="cancellationToken">Токен отмены для прерывания операции</param>
        /// <param name="initialMessage">Сообщение, инициировавшее сессию</param>
        Task StartAsync(CancellationToken cancellationToken, BotMessage initialMessage);

        /// <summary>
        /// Останавливает сессию и освобождает ресурсы
        /// </summary>
        Task StopAsync();
    }
}
