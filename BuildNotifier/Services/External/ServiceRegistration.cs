using BuildNotifier.Data.Models.ServiceRegistration;
using Confluent.Kafka;
using System.Text.Json;

namespace BuildNotifier.Services.External
{
    /// <summary>
    /// Сервис для регистрации core сервиса в командном менеджере
    /// </summary>
    public class ServiceRegistration
    {
        private readonly ProducerConfig _producerConfig;
        private readonly ConsumerConfig _consumerConfig;
        private readonly ServiceDescription _serviceDescription;

        /// <summary>
        /// Сервис для регистрации core сервиса в командном менеджере
        /// </summary>
        /// <param name="serviceDescription">Описание названия сервиса и его команд</param>
        /// <param name="producerConfig">Конфигурация consumer для Kafka</param>
        /// <param name="consumerConfig">Конфигурация producer для Kafka</param>
        public ServiceRegistration(
            ServiceDescription serviceDescription, 
            ProducerConfig producerConfig, 
            ConsumerConfig consumerConfig
            )
        {
            _producerConfig = producerConfig;
            _consumerConfig = consumerConfig;
            _serviceDescription = serviceDescription;
        }


        /// <summary>
        /// Регистрирует сервис в командном менеджере
        /// </summary>
        /// <param name="cancellationToken">Токен для завершения работы сервиса</param>
        /// <returns>Информация о топиках для отправки и получения сообщений</returns>
        public async Task<ServiceRegistrationInfo?> RegisterService(CancellationToken cancellationToken)
        {
            var _producer = new ProducerBuilder<Null, string>(_producerConfig).Build();
            var _consumer = new ConsumerBuilder<Null, string>(_consumerConfig).Build();
            _consumer.Subscribe("service-info-response");
            try
            {
                // Запрос на регистрацию сервиса в командном менеджере
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var json = JsonSerializer.Serialize(_serviceDescription);
                        var message = new Message<Null, string> { Value = json };

                        var deliveryResult = await _producer.ProduceAsync("service-info-request", message);
                        Console.WriteLine($"Сообщение доставлено в {deliveryResult.Topic}.");

                        // Получение ответа от командного менеджера
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                var receiptResult = _consumer.Consume(TimeSpan.FromSeconds(30));

                                if (receiptResult != null)
                                {
                                    Console.WriteLine($"Получено сообщение от {receiptResult.Topic}.");

                                    var serviceRegistrationInfo = JsonSerializer.Deserialize<ServiceRegistrationInfo>(receiptResult.Message.Value);

                                    if (serviceRegistrationInfo?.ServiceName == _serviceDescription.Name)
                                    { 
                                        return serviceRegistrationInfo; 
                                    }
                                    else
                                    {
                                        Console.WriteLine("Получено сообщение для другого сервиса.");
                                        Console.WriteLine("Повторная попытка получения.");
                                    }
                                }
                                else
                                {
                                    throw new TimeoutException("Время вышло.");
                                }
                            }
                            catch (TimeoutException e)
                            {
                                Console.WriteLine(e.Message);
                                Console.WriteLine("Повторная попытка получения.");
                            }
                        }
                    }
                    catch (ProduceException<Null, string> e)
                    {
                        Console.WriteLine($"Не удалось доставить сообщение: {e.Error.Reason}.");
                        Console.WriteLine("Повторная попытка доставки.");
                    }
                    catch (ConsumeException e)
                    {
                        Console.WriteLine($"Не удалось получить сообщение: {e.Error.Reason}.");
                        Console.WriteLine("Повторная попытка получения.");
                    }
                }
            }
            finally
            {
                _producer?.Dispose();
                _consumer?.Dispose();
            }
            return null;
        }
    }
}
