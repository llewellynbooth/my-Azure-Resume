using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Company.Function;

public class ContactForm
{
    private const int MaxPerWindow = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly MessageStore _store;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ContactForm> _logger;

    public ContactForm(MessageStore store, IMemoryCache cache, ILogger<ContactForm> logger)
    {
        _store = store;
        _cache = cache;
        _logger = logger;
    }

    [Function("ContactForm")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")] HttpRequest req,
        CancellationToken ct)
    {
        var ip = ClientIp(req);

        if (!AllowRequest(ip))
        {
            _logger.LogWarning("Contact form rate limit hit for {Ip}", ip);
            return new ObjectResult(new { error = "Too many requests. Please try again later." })
            {
                StatusCode = StatusCodes.Status429TooManyRequests
            };
        }

        ContactRequest? data;
        try
        {
            data = await JsonSerializer.DeserializeAsync<ContactRequest>(req.Body, JsonOpts, ct);
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new { error = "Request body is not valid JSON." });
        }

        var result = ContactValidator.Validate(data, ip);

        if (result.Outcome == ContactOutcome.Honeypot)
        {
            _logger.LogWarning("Contact form honeypot triggered from {Ip}", ip);
            return new OkObjectResult(new { success = true, message = "Thanks — your message has been received." });
        }

        if (result.Outcome == ContactOutcome.Invalid)
            return new BadRequestObjectResult(new { error = result.Error });

        await _store.AddAsync(result.Message!, ct);
        _logger.LogInformation("Contact message stored from {Email}", result.Message!.Email);

        return new OkObjectResult(new
        {
            success = true,
            message = "Thank you for your message! I'll get back to you soon.",
            id = result.Message!.Id
        });
    }

    // Best-effort, per-instance. A distributed limiter is a follow-up (needs shared state).
    private bool AllowRequest(string ip)
    {
        var key = $"contact-rl:{ip}";
        var count = _cache.TryGetValue(key, out int existing) ? existing : 0;

        if (count >= MaxPerWindow)
            return false;

        _cache.Set(key, count + 1, Window);
        return true;
    }

    // Behind Azure's edge, RemoteIpAddress is the platform address — the real client
    // is the first hop of X-Forwarded-For (App Service appends ":port" for IPv4).
    private static string ClientIp(HttpRequest req)
    {
        var forwarded = req.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',')[0].Trim();
            if (first.Count(c => c == ':') == 1)
                first = first[..first.IndexOf(':')]; // strip ":port" from IPv4
            return first;
        }
        return req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
