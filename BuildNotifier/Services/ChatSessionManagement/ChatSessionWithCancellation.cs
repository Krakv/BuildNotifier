using BuildNotifier.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildNotifier.Services.ChatSessionManagement
{
    /// <summary>
    /// Класс-обертка для управления сессией чата с возможностью отмены операций
    /// </summary>
    /// <remarks>
    /// Объединяет экземпляр сессии чата (<see cref="IChatSession"/>) 
    /// с источником токена отмены (<see cref="CancellationTokenSource"/>).
    /// </remarks>
    public sealed class ChatSessionWithCancellation
    {
        /// <summary>
        /// Источник токена отмены для операций в рамках сессии
        /// </summary>
        /// <value>
        /// Экземпляр <see cref="CancellationTokenSource"/>, связанный с сессией.
        /// Позволяет отменить все асинхронные операции сессии.
        /// </value>
        public CancellationTokenSource CancellationTokenSource { get; } = new();

        /// <summary>
        /// Экземпляр сессии чата
        /// </summary>
        /// <value>
        /// Реализация интерфейса <see cref="IChatSession"/>, 
        /// представляющая конкретную сессию чата.
        /// </value>
        public IChatSession Session { get; }

        /// <summary>
        /// Инициализирует новый экземпляр класса ChatSessionWithCancellation
        /// </summary>
        /// <param name="session">Экземпляр сессии чата</param>
        /// <exception cref="ArgumentNullException">
        /// Выбрасывается, если параметр <paramref name="session"/> равен null
        /// </exception>
        public ChatSessionWithCancellation(IChatSession session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>
        /// Выполняет отмену всех операций сессии и освобождает ресурсы
        /// </summary>
        /// <remarks>
        /// Последовательность операций:
        /// 1. Инициирует отмену через <see cref="CancellationTokenSource.Cancel"/>
        /// 2. Освобождает ресурсы через <see cref="CancellationTokenSource.Dispose"/>
        /// </remarks>
        public void CancelAndDispose()
        {
            try
            {
                CancellationTokenSource.Cancel();
            }
            finally
            {
                CancellationTokenSource.Dispose();
            }
        }
    }
}
