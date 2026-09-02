using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function;

public class GetResumeFunction
{
    private readonly CounterStore _counter;
    private readonly ILogger<GetResumeFunction> _logger;

    public GetResumeFunction(CounterStore counter, ILogger<GetResumeFunction> logger)
    {
        _counter = counter;
        _logger = logger;
    }

    // GET returns the current count; POST increments it. (The site POSTs.)
    [Function("getResumeFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
        CancellationToken ct)
    {
        var count = HttpMethods.IsPost(req.Method)
            ? await _counter.IncrementAsync(ct)
            : await _counter.GetAsync(ct);

        _logger.LogInformation("getResumeFunction {Method} -> {Count}", req.Method, count);

        return new OkObjectResult(new Counter { Id = Db.CounterId, Count = count });
    }
}
