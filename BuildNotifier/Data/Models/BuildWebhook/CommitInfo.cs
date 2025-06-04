using System.Text.Json.Serialization;

namespace BuildNotifier.Data.Models.BambooWebhookPayload
{
    /// <summary>
    /// Класс, содержащий информацию о коммите.
    /// </summary>
    public class CommitInfo
    {
        /// <summary>
        /// Получает или задает хеш коммита.
        /// </summary>
        /// <value>
        /// Строка с хешем коммита. Обязательное поле.
        /// </value>
        public required string Hash { get; set; }

        /// <summary>
        /// Получает или задает логин автора коммита.
        /// </summary>
        /// <value>
        /// Строка с логином автора. Обязательное поле.
        /// </value>
        public required string Author { get; set; }

        /// <summary>
        /// Получает или задает сообщение коммита.
        /// </summary>
        /// <value>
        /// Строка с сообщением коммита. Обязательное поле.
        /// </value>
        public required string Message { get; set; }
    }

}
