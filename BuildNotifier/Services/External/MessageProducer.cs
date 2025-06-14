﻿using BuildNotifier.Data.Models.ServiceRegistration;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace BuildNotifier.Services.External
{
    /// <summary>
    /// Сервис для отправки сообщений в Kafka
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр продюсера сообщений
    /// </remarks>
    /// <param name="producerConfig">Конфигурация producer для Kafka</param>
    /// <param name="serviceRegistrationInfo">Информация о сервисе (содержит топик для отправки)</param>
    /// <param name="logger">Логгер для записи событий</param>
    public class MessageProducer(
        IOptions<ProducerConfig> producerConfig,
        ServiceRegistrationInfo serviceRegistrationInfo,
        ILogger<MessageProducer> logger) : IDisposable
    {
        private readonly IProducer<Null, string> _producer = new ProducerBuilder<Null, string>(producerConfig?.Value ?? throw new ArgumentNullException(nameof(producerConfig)))
                .Build();
        private readonly ServiceRegistrationInfo _serviceRegistrationInfo = serviceRegistrationInfo ?? throw new ArgumentNullException(nameof(serviceRegistrationInfo));
        private readonly ILogger<MessageProducer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// Асинхронно отправляет сообщение в Kafka
        /// </summary>
        /// <param name="message">Сообщение для отправки (в формате JSON)</param>
        public async Task SendRequest(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                _logger.LogWarning("Попытка отправить пустое сообщение");
                return;
            }

            try
            {
                var result = await _producer.ProduceAsync(
                    _serviceRegistrationInfo.ProduceTopic,
                    new Message<Null, string> { Value = message });

                _logger.LogInformation("[{Topic}] Сообщение отправлено: {Message}",
                    _serviceRegistrationInfo.ProduceTopic,
                    result.Message.Value);
            }
            catch (ProduceException<Null, string> ex)
            {
                _logger.LogError(ex, "[{Topic}] Ошибка при отправке сообщения",
                    _serviceRegistrationInfo.ProduceTopic);
            }
        }

        /// <summary>
        /// Освобождает ресурсы продюсера Kafka
        /// </summary>
        public void Dispose()
        {
            try
            {
                _producer?.Flush(TimeSpan.FromSeconds(5));
                _producer?.Dispose();
                _logger.LogInformation("Продюсер Kafka корректно остановлен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при освобождении ресурсов продюсера");
            }
        }
    }
}
