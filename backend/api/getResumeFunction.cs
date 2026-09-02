using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function;

public class GetResumeFunction
{
    private readonly Container _counter;
    private readonly ILogger<GetResumeFunction> _logger;

    public GetResumeFunction(CosmosClient cosmos, ILogger<GetResumeFunction> logger)
    {
        _counter = cosmos.GetContainer("CloudResume", "Counter");
        _logger = logger;
    }

    [Function("getResumeFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("getResumeFunction triggered.");

        Counter counter;
        try
        {
            var read = await _counter.ReadItemAsync<Counter>("index", new PartitionKey("index"));
            counter = read.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            counter = new Counter { Id = "index", Count = 0 };
        }

        counter.Count += 1;
        await _counter.UpsertItemAsync(counter, new PartitionKey("index"));

        return new OkObjectResult(counter);
    }
}
