using System.Text.Json;
using Company.Function;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    // ConfigureFunctionsWebApplication() (not ...WorkerDefaults) enables the ASP.NET Core
    // integration, so functions take HttpRequest and return IActionResult.
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton(_ =>
        {
            var connection = Environment.GetEnvironmentVariable(Db.ConnectionSetting)
                ?? throw new InvalidOperationException(
                    $"App setting '{Db.ConnectionSetting}' (Cosmos connection string) is not set.");
            return new CosmosClient(connection, new CosmosClientOptions
            {
                // Honour the [JsonPropertyName] attributes on the models
                // (v3 CosmosClient uses Newtonsoft by default).
                UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions()
            });
        });

        services.AddSingleton<CounterStore>();
        services.AddSingleton<MessageStore>();
        services.AddSingleton<ContactNotifier>();
        services.AddMemoryCache(); // best-effort per-instance rate limiting for /api/contact

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();
