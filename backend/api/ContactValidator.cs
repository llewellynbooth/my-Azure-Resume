using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Company.Function;

/// <summary>What the client POSTs to /api/contact. Bound case-insensitively.</summary>
public class ContactRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public string? Website { get; set; } // honeypot — real users leave this empty
}

/// <summary>The document stored in the Messages container.</summary>
public class ContactMessage
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("email")] public string Email { get; set; } = "";
    [JsonPropertyName("subject")] public string Subject { get; set; } = "";
    [JsonPropertyName("message")] public string Message { get; set; } = "";
    [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("ipAddress")] public string IpAddress { get; set; } = "unknown";
}

public enum ContactOutcome { Valid, Invalid, Honeypot }

public record ContactValidation(ContactOutcome Outcome, string? Error, ContactMessage? Message);

/// <summary>Pure validation for contact-form submissions — no I/O, unit-tested directly.</summary>
public static class ContactValidator
{
    public const int NameMax = 100;
    public const int EmailMax = 200;
    public const int SubjectMax = 150;
    public const int MessageMax = 5000;

    private static readonly Regex EmailPattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);

    public static ContactValidation Validate(ContactRequest? data, string ipAddress)
    {
        if (!string.IsNullOrWhiteSpace(data?.Website))
            return new(ContactOutcome.Honeypot, null, null);

        var name = data?.Name?.Trim() ?? "";
        var email = data?.Email?.Trim() ?? "";
        var subject = string.IsNullOrWhiteSpace(data?.Subject) ? "Contact form submission" : data!.Subject!.Trim();
        var message = data?.Message?.Trim() ?? "";

        if (name.Length == 0 || email.Length == 0 || message.Length == 0)
            return new(ContactOutcome.Invalid, "Name, email, and message are required.", null);

        if (name.Length > NameMax || email.Length > EmailMax ||
            subject.Length > SubjectMax || message.Length > MessageMax)
            return new(ContactOutcome.Invalid, "One or more fields exceed the allowed length.", null);

        if (!EmailPattern.IsMatch(email))
            return new(ContactOutcome.Invalid, "Invalid email address.", null);

        // HTML-encode stored text so it is inert if ever rendered in an admin view.
        return new(ContactOutcome.Valid, null, new ContactMessage
        {
            Name = WebUtility.HtmlEncode(name),
            Email = WebUtility.HtmlEncode(email),
            Subject = WebUtility.HtmlEncode(subject),
            Message = WebUtility.HtmlEncode(message),
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress
        });
    }
}
