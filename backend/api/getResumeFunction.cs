using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Company.Function;

public class GetResumeFunction
{
    private readonly ILogger<GetResumeFunction> _logger;

    public GetResumeFunction(ILogger<GetResumeFunction> logger) => _logger = logger;

    // Reads the single "index" counter document, increments it, and upserts it back
    // via the Cosmos output binding on the response object.
    [Function("getResumeFunction")]
    public CounterResponse Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
        [CosmosDBInput(
            databaseName: "CloudResume",
            containerName: "Counter",
            Connection = "CloudResume",
            Id = "index",
            PartitionKey = "index")] Counter counter)
    {
        _logger.LogInformation("getResumeFunction triggered.");

        counter.Count += 1;

        return new CounterResponse
        {
            UpdatedCounter = counter,
            HttpResponse = new OkObjectResult(counter)
        };
    }
}

public class CounterResponse
{
    [CosmosDBOutput(
        databaseName: "CloudResume",
        containerName: "Counter",
        Connection = "CloudResume")]
    public Counter? UpdatedCounter { get; set; }

    public IActionResult HttpResponse { get; set; } = new OkResult();
}
