using BuildNotifier.Services.Interfaces;

namespace BuildNotifier.Services.ChatSessionManagement
{
    /// <summary>
    /// Фабрика для создания сессий уведомлений
    /// </summary>
    public class NotifierSubscriptionFactory : IChatSessionFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Инициализирует фабрику с провайдером сервисов
        /// </summary>
        /// <param name="serviceProvider">DI-контейнер для разрешения зависимостей</param>
        public NotifierSubscriptionFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Создает новую сессию с использованием DI-контейнера
        /// </summary>
        /// <returns>Экземпляр сессии чата</returns>
        public IChatSession CreateSession()
        {
            using var scope = _serviceProvider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IChatSession>();
        }
    }
}
