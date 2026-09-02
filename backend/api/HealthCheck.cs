using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function;

public class HealthCheck
{
    private readonly ILogger<HealthCheck> _logger;

    public HealthCheck(ILogger<HealthCheck> logger) => _logger = logger;

    [Function("HealthCheck")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req,
        [CosmosDBInput(
            databaseName: "CloudResume",
            containerName: "Counter",
            Connection = "CloudResume",
            Id = "index",
            PartitionKey = "index")] Counter? counter)
    {
        _logger.LogInformation("Health check endpoint called.");

        return new OkObjectResult(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Azure Resume API",
            version = "1.0.0",
            checks = new
            {
                database = counter is not null ? "connected" : "disconnected",
                api = "operational"
            }
        });
    }
}
