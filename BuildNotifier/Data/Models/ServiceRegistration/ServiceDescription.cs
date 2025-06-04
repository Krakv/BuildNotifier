using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.ServiceRegistration
{
    /// <summary>
    /// Описывает сервис и его команды.
    /// </summary>
    public class ServiceDescription
    {
        /// <summary>
        /// Получает или задает имя сервиса.
        /// </summary>
        /// <value>Уникальное имя сервиса.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Получает или задает описание сервиса.
        /// </summary>
        /// <value>Подробное описание функциональности сервиса.</value>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Получает или задает список команд, поддерживаемых сервисом.
        /// </summary>
        /// <value>Коллекция объектов <see cref="ServiceCommand"/>.</value>
        public List<ServiceCommand> Commands { get; set; } = null!;
    }
}
