using System.Text.Json;
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
            var connection = Environment.GetEnvironmentVariable("CloudResume")
                ?? throw new InvalidOperationException("App setting 'CloudResume' (Cosmos connection string) is not set.");
            return new CosmosClient(connection, new CosmosClientOptions
            {
                // Honour the [JsonPropertyName] attributes on the models.
                UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions()
            });
        });

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();
