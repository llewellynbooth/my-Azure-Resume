using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function;

public class ContactMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = "unknown";
}

// Shape the client sends. Bound case-insensitively.
public class ContactRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public string? Website { get; set; } // honeypot — real users leave this empty
}

public class ContactForm
{
    private const int NameMax = 100;
    private const int EmailMax = 200;
    private const int SubjectMax = 150;
    private const int MessageMax = 5000;

    private static readonly Regex EmailPattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private readonly Container _messages;
    private readonly ILogger<ContactForm> _logger;

    public ContactForm(CosmosClient cosmos, ILogger<ContactForm> logger)
    {
        _messages = cosmos.GetContainer("CloudResume", "Messages");
        _logger = logger;
    }

    [Function("ContactForm")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")] HttpRequest req)
    {
        _logger.LogInformation("Contact form submission received.");

        ContactRequest? data;
        try
        {
            data = await JsonSerializer.DeserializeAsync<ContactRequest>(req.Body, JsonOpts);
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new { error = "Request body is not valid JSON." });
        }

        // Honeypot: silently accept and drop bot submissions.
        if (!string.IsNullOrWhiteSpace(data?.Website))
        {
            _logger.LogWarning("Contact form honeypot triggered; dropping submission.");
            return new OkObjectResult(new { success = true, message = "Thanks — your message has been received." });
        }

        var name = data?.Name?.Trim() ?? "";
        var email = data?.Email?.Trim() ?? "";
        var subject = string.IsNullOrWhiteSpace(data?.Subject) ? "Contact form submission" : data!.Subject!.Trim();
        var message = data?.Message?.Trim() ?? "";

        if (name.Length == 0 || email.Length == 0 || message.Length == 0)
            return new BadRequestObjectResult(new { error = "Name, email, and message are required." });

        if (name.Length > NameMax || email.Length > EmailMax ||
            subject.Length > SubjectMax || message.Length > MessageMax)
            return new BadRequestObjectResult(new { error = "One or more fields exceed the allowed length." });

        if (!EmailPattern.IsMatch(email))
            return new BadRequestObjectResult(new { error = "Invalid email address." });

        var contactMessage = new ContactMessage
        {
            // HTML-encode stored text so it is inert if ever rendered in an admin view.
            Name = WebUtility.HtmlEncode(name),
            Email = WebUtility.HtmlEncode(email),
            Subject = WebUtility.HtmlEncode(subject),
            Message = WebUtility.HtmlEncode(message),
            Timestamp = DateTime.UtcNow,
            IpAddress = req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        };

        await _messages.CreateItemAsync(contactMessage, new PartitionKey(contactMessage.Id));
        _logger.LogInformation("Contact message stored from {Email}", contactMessage.Email);

        return new OkObjectResult(new
        {
            success = true,
            message = "Thank you for your message! I'll get back to you soon.",
            id = contactMessage.Id
        });
    }
}
