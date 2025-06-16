

using DataAnalystBackend.Shared.DataAccess;
using DataAnalystBackend.Shared.Interfaces;
using DataAnalystBackend.Shared.Interfaces.Services;
using DataAnalystBackend.Shared.MessagingProviders;
using DataAnalystBackend.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>(true, false);

if (File.Exists("/app/config/appsettings.production.json"))
    builder.Configuration.AddJsonFile("/app/config/appsettings.production.json", optional: false, reloadOnChange: false);
else
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

Console.WriteLine($"environment: {builder.Environment.EnvironmentName}");

var factory = new ConnectionFactory { HostName = builder.Configuration.GetValue<string>("RabbitMQ:HostName") };
string? username = builder.Configuration.GetValue<string>("RabbitMQ:Username");
string? password = builder.Configuration.GetValue<string>("RabbitMQ:Password");
if (!string.IsNullOrWhiteSpace(username))
    factory.UserName = username;

if (!string.IsNullOrWhiteSpace(password))
    factory.Password = password;

builder.Services.AddTransient<IMessagingProvider, RabbitMQMessagingProvider>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddTransient<IDataSessionService, DataSessionService>();
builder.Services.AddHttpClient("AgentClient", httpClient =>
{
    httpClient.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Agent:URL"));
    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
});

using IHost app = builder.Build();

var handlers = AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(o => o.GetTypes())
    .Where(o => typeof(IMessageQueueHandler).IsAssignableFrom(o) && o.IsClass && !o.IsAbstract);

List<Task> consumers = new List<Task>();

foreach (var handler in handlers)
{
    var handlerInstance = (IMessageQueueHandler)Activator.CreateInstance(handler);
    await handlerInstance.InitializeQueue(factory, app.Services, builder.Configuration);
    consumers.Add(handlerInstance.StartConsuming());
}
var shutdown = new ManualResetEventSlim(false);

AppDomain.CurrentDomain.ProcessExit += (s, e) => shutdown.Set();
Console.CancelKeyPress += (s, e) => {
    e.Cancel = true;
    shutdown.Set();
};

await Task.WhenAll(consumers);

Console.WriteLine("Service running. Waiting for shutdown signal...");
shutdown.Wait();

foreach (var consumer in consumers)
{
    consumer.Dispose();
}