namespace BuildNotifier.Services.Interfaces
{
    // <summary>
    /// Фабрика для создания сессий чата
    /// </summary>
    public interface IChatSessionFactory
    {
        /// <summary>
        /// Создает новую сессию чата с указанным идентификатором
        /// </summary>
        /// <returns>Экземпляр сессии чата, реализующий <see cref="IChatSession"/></returns>
        IChatSession CreateSession();
    }
}
