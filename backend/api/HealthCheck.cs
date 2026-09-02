using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function;

public class HealthCheck
{
    private readonly Container _counter;
    private readonly ILogger<HealthCheck> _logger;

    public HealthCheck(CosmosClient cosmos, ILogger<HealthCheck> logger)
    {
        _counter = cosmos.GetContainer("CloudResume", "Counter");
        _logger = logger;
    }

    [Function("HealthCheck")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
    {
        _logger.LogInformation("Health check endpoint called.");

        string database;
        try
        {
            await _counter.ReadItemAsync<Counter>("index", new PartitionKey("index"));
            database = "connected";
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            database = "connected"; // reachable, document just missing
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check Cosmos read failed.");
            database = "disconnected";
        }

        return new OkObjectResult(new
        {
            status = database == "connected" ? "healthy" : "degraded",
            timestamp = DateTime.UtcNow,
            service = "Azure Resume API",
            version = "1.0.0",
            checks = new { database, api = "operational" }
        });
    }
}
