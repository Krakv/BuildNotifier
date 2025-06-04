using BuildNotifier.Data.Context;
using BuildNotifier.Data.Models.HTTPClient;
using BuildNotifier.Data.Models.Kafka;
using BuildNotifier.Data.Models.ServiceRegistration;
using BuildNotifier.Data.Repositories;
using BuildNotifier.Services;
using BuildNotifier.Services.ChatSessionManagement;
using BuildNotifier.Services.External;
using BuildNotifier.Services.Interfaces;
using BuildNotifier.Services.Startup;
using Confluent.Kafka;
using System.Collections.Concurrent;

var preBuilderConfig = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

using var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;
ServiceRegistrationInfo serviceRegistrationInfo;

try
{
    serviceRegistrationInfo = await RegisterServiceAsync(preBuilderConfig, cancellationToken);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Регистрация сервиса отменена");
    throw;
}

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        ConfigureServices(services, serviceRegistrationInfo, hostContext.Configuration);
    });

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

await host.RunAsync(cancellationToken);

static async Task<ServiceRegistrationInfo> RegisterServiceAsync(IConfiguration preBuilderConfig, CancellationToken cancellationToken)
{
    var serviceDescription = preBuilderConfig
        .GetSection("TelegramBotService")
        .Get<ServiceDescription>();
    var consumerConfig = preBuilderConfig
        .GetSection("Kafka:ConsumerConfig:ServiceRegistration")
        .Get<ConsumerConfig>();
    var producerConfig = preBuilderConfig
        .GetSection("Kafka:ProducerConfig:ServiceRegistration")
        .Get<ProducerConfig>();

    if (serviceDescription == null)
        throw new InvalidOperationException("Конфигурация для TelegramBotService в appsettings.json не найдена");
    if (consumerConfig == null)
        throw new InvalidOperationException("Конфигурация ConsumerConfig в appsettings.json не найдена");
    if (producerConfig == null)
        throw new InvalidOperationException("Конфигурация ProducerConfig в appsettings.json не найдена");

    var serviceRegistrationInfo = await new ServiceRegistration(serviceDescription, producerConfig, consumerConfig)
        .RegisterService(cancellationToken);

    return serviceRegistrationInfo ?? throw new InvalidOperationException("Данные о регистрации сервиса не получены.");
}

static void ConfigureServices(IServiceCollection services, ServiceRegistrationInfo serviceRegistrationInfo, IConfiguration configuration)
{
    services.Configure<ServiceRegistrationInfo>(configuration.GetSection("TelegramBotService"));
    services.Configure<ConsumerConfig>(configuration.GetSection("Kafka:ConsumerConfig:Default"));
    services.Configure<ProducerConfig>(configuration.GetSection("Kafka:ProducerConfig:Default"));
    services.Configure<TelegramNicknameService>(configuration.GetSection("TelegramNicknameService"));
    services.Configure<KafkaTopics>(configuration.GetSection("Kafka:Topics"));

    services.AddDbContextFactory<AppDbContext>();

    services.AddSingleton<IChatSessionFactory, NotifierSubscriptionFactory>();
    services.AddSingleton<ChatSessionManager>();
    services.AddSingleton<PlanChatRepository>();
    services.AddSingleton(new ServiceDescription());
    services.AddSingleton<ConcurrentDictionary<string, string>>();
    services.AddSingleton(serviceRegistrationInfo);
    services.AddSingleton<TelegramNotificationService>();
    services.AddSingleton<MessageProducer>();
    services.AddSingleton<SubscriptionService>();
    services.AddScoped<IChatSession, SubscriptionWithSessionService>();

    services.AddHostedService<CommandManagerListenerService>();
    services.AddHttpClient<ApiHttpClient>();
}