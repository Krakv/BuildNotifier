using BuildNotifier.Data.Context;
using BuildNotifier.Data.Models.HTTPClient;
using BuildNotifier.Data.Models.ServiceRegistration;
using BuildNotifier.Data.Repositories;
using BuildNotifier.Services;
using BuildNotifier.Services.External;
using BuildNotifier.Services.Interfaces;
using Confluent.Kafka;
using System.Collections.Concurrent;

var preBuilderConfig = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(300));
var cancellationToken = cts.Token;
ServiceRegistrationInfo serviceRegistrationInfo;

try
{
    serviceRegistrationInfo = await RegisterServiceAsync(preBuilderConfig, cancellationToken);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Регистрация сервиса отменена или превышен таймаут");
    throw;
}

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder, serviceRegistrationInfo);

var app = builder.Build();
ConfigureMiddleware(app);

app.Run();

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
        throw new InvalidOperationException("No configuration found for TelegramBotService in appsettings.json");
    if (consumerConfig == null)
        throw new InvalidOperationException("No configuration found for ConsumerConfig in appsettings.json");
    if (producerConfig == null)
        throw new InvalidOperationException("No configuration found for ProducerConfig in appsettings.json");

    var serviceRegistrationInfo = await new ServiceRegistration(serviceDescription, producerConfig, consumerConfig)
        .RegisterService(cancellationToken);

    return serviceRegistrationInfo ?? throw new InvalidOperationException("No service registration data received.");
}

static void ConfigureServices(WebApplicationBuilder builder, ServiceRegistrationInfo serviceRegistrationInfo)
{
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Конфигурации
    builder.Services.Configure<ServiceRegistrationInfo>(builder.Configuration.GetSection("TelegramBotService"));
    builder.Services.Configure<ConsumerConfig>(builder.Configuration.GetSection("Kafka:ConsumerConfig:Default"));
    builder.Services.Configure<ProducerConfig>(builder.Configuration.GetSection("Kafka:ProducerConfig:Default"));
    builder.Services.Configure<HttpUrl>(builder.Configuration.GetSection("Kafka:HttpUrl"));

    // База данных
    builder.Services.AddDbContextFactory<AppDbContext>();

    // Сервисы
    builder.Services.AddSingleton<IChatSessionFactory, NotifierSubscriptionFactory>();
    builder.Services.AddSingleton<ChatSessionManager>();
    builder.Services.AddScoped<IChatSession, NotifierSubscriptionService>();
    builder.Services.AddSingleton<PlanChatRepository>();
    builder.Services.AddSingleton(new ServiceDescription());
    builder.Services.AddSingleton<ConcurrentDictionary<string, string>>();
    builder.Services.AddSingleton(serviceRegistrationInfo);
    builder.Services.AddSingleton<TelegramNotificationService>();
    builder.Services.AddSingleton<MessageProducer>();

    builder.Services.AddHostedService<CommandManagerListenerService>();
    builder.Services.AddHttpClient<ApiHttpClient>();

    // Логирование
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    builder.WebHost.UseUrls("http://0.0.0.0:5000", "https://0.0.0.0:5001");
}

static void ConfigureMiddleware(WebApplication app)
{
    app.UseCors("AllowAll");

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthorization();
    app.MapControllers();
}