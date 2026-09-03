using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Company.Function;

/// <summary>
/// Verifies a Cloudflare Turnstile token server-side. No-ops (allows) until
/// TURNSTILE_SECRET is configured, so the app deploys before setup is done.
/// Fails closed on error — a rejected submission is safer than an open form.
/// </summary>
public class TurnstileVerifier
{
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private readonly HttpClient _http;
    private readonly string? _secret;
    private readonly ILogger<TurnstileVerifier> _logger;

    public TurnstileVerifier(HttpClient http, ILogger<TurnstileVerifier> logger)
    {
        _http = http;
        _logger = logger;
        _secret = Environment.GetEnvironmentVariable("TURNSTILE_SECRET");
    }

    private bool Enabled => !string.IsNullOrWhiteSpace(_secret);

    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct)
    {
        if (!Enabled)
        {
            _logger.LogWarning("TURNSTILE_SECRET not set — skipping bot verification.");
            return true;
        }

        if (string.IsNullOrWhiteSpace(token))
            return false;

        var form = new List<KeyValuePair<string, string>>
        {
            new("secret", _secret!),
            new("response", token)
        };
        if (!string.IsNullOrWhiteSpace(remoteIp) && remoteIp != "unknown")
            form.Add(new("remoteip", remoteIp));

        try
        {
            using var resp = await _http.PostAsync(VerifyUrl, new FormUrlEncodedContent(form), ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            var ok = doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean();
            if (!ok)
                _logger.LogWarning("Turnstile verification rejected: {Body}", body);
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Turnstile verification call failed — rejecting submission.");
            return false;
        }
    }
}
