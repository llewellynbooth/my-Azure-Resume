using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function;

public class HealthCheck
{
    private readonly CounterStore _counter;
    private readonly ILogger<HealthCheck> _logger;

    public HealthCheck(CounterStore counter, ILogger<HealthCheck> logger)
    {
        _counter = counter;
        _logger = logger;
    }

    [Function("HealthCheck")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req,
        CancellationToken ct)
    {
        _logger.LogInformation("Health check endpoint called.");

        var connected = await _counter.PingAsync(ct);

        return new OkObjectResult(new
        {
            status = connected ? "healthy" : "degraded",
            timestamp = DateTime.UtcNow,
            service = "Azure Resume API",
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
            checks = new
            {
                database = connected ? "connected" : "disconnected",
                api = "operational"
            }
        });
    }
}
