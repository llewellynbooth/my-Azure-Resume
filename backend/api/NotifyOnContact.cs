using System.Net;
using Azure;
using Azure.Communication.Email;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function;

/// <summary>
/// Fires on the Cosmos change feed for the Messages container and emails a notification
/// for each new contact-form submission. Decoupled from the HTTP path so a slow or failed
/// email never affects the visitor's request.
/// </summary>
public class NotifyOnContact
{
    private readonly ContactNotifier _notifier;
    private readonly ILogger<NotifyOnContact> _logger;

    public NotifyOnContact(ContactNotifier notifier, ILogger<NotifyOnContact> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    [Function("NotifyOnContact")]
    public async Task Run(
        [CosmosDBTrigger(
            databaseName: Db.Database,
            containerName: Db.MessagesContainer,
            Connection = Db.ConnectionSetting,
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<ContactMessage> messages,
        CancellationToken ct)
    {
        if (messages is null || messages.Count == 0)
            return;

        _logger.LogInformation("NotifyOnContact: {Count} new message(s).", messages.Count);
        foreach (var message in messages)
            await _notifier.NotifyAsync(message, ct);
    }
}

/// <summary>
/// Sends a plain-text notification email via Azure Communication Services. No-ops (with a
/// warning) until the ACS app settings are configured, so the app runs before setup is done.
/// </summary>
public class ContactNotifier
{
    private readonly EmailClient? _client;
    private readonly string? _sender;     // e.g. donotreply@<guid>.azurecomm.net
    private readonly string? _recipient;  // where you want to be notified
    private readonly ILogger<ContactNotifier> _logger;

    public ContactNotifier(ILogger<ContactNotifier> logger)
    {
        _logger = logger;
        _sender = Environment.GetEnvironmentVariable("NOTIFY_SENDER_ADDRESS");
        _recipient = Environment.GetEnvironmentVariable("NOTIFY_EMAIL");

        var connection = Environment.GetEnvironmentVariable("ACS_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(connection))
        {
            try { _client = new EmailClient(connection); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to create EmailClient from ACS_CONNECTION_STRING."); }
        }
    }

    private bool Configured =>
        _client is not null && !string.IsNullOrWhiteSpace(_sender) && !string.IsNullOrWhiteSpace(_recipient);

    public async Task NotifyAsync(ContactMessage m, CancellationToken ct)
    {
        if (!Configured)
        {
            _logger.LogWarning(
                "ContactNotifier not configured (ACS_CONNECTION_STRING / NOTIFY_SENDER_ADDRESS / NOTIFY_EMAIL) — no email sent for {Id}.",
                m.Id);
            return;
        }

        // Stored fields are HTML-encoded; decode for a readable plain-text email.
        var name = WebUtility.HtmlDecode(m.Name);
        var email = WebUtility.HtmlDecode(m.Email);
        var subject = WebUtility.HtmlDecode(m.Subject);
        var body = WebUtility.HtmlDecode(m.Message);

        var content = new EmailContent($"Contact form: {subject}")
        {
            PlainText =
                $"From:  {name} <{email}>\n" +
                $"When:  {m.Timestamp:u}\n" +
                $"IP:    {m.IpAddress}\n" +
                $"\n{body}\n"
        };

        var message = new EmailMessage(_sender!, _recipient!, content);
        message.ReplyTo.Add(new EmailAddress(email, name));   // hit reply -> goes to the sender

        try
        {
            await _client!.SendAsync(WaitUntil.Completed, message, ct);
            _logger.LogInformation("Contact notification sent for {Id} to {Recipient}.", m.Id, _recipient);
        }
        catch (Exception ex)
        {
            // Don't rethrow — a failed email must not block the change-feed checkpoint for
            // later messages. The submission is always persisted in Cosmos; failures surface
            // in Application Insights (set an alert on NotifyOnContact exceptions).
            _logger.LogError(ex, "Failed to send contact notification for {Id}.", m.Id);
        }
    }
}
