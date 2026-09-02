using Xunit;

namespace Company.Function;

public class ContactValidatorTests
{
    private static ContactRequest Req(
        string? name = "Sam Tester",
        string? email = "sam@example.com",
        string? message = "Hello, this is a genuine message.",
        string? subject = null,
        string? website = null) =>
        new() { Name = name, Email = email, Message = message, Subject = subject, Website = website };

    [Fact]
    public void Valid_input_passes_and_builds_message()
    {
        var r = ContactValidator.Validate(Req(), "203.0.113.7");

        Assert.Equal(ContactOutcome.Valid, r.Outcome);
        Assert.NotNull(r.Message);
        Assert.Equal("sam@example.com", r.Message!.Email);
        Assert.Equal("Contact form submission", r.Message.Subject); // default applied
        Assert.Equal("203.0.113.7", r.Message.IpAddress);
    }

    [Fact]
    public void Honeypot_field_present_is_flagged()
    {
        var r = ContactValidator.Validate(Req(website: "http://spam.example"), "x");
        Assert.Equal(ContactOutcome.Honeypot, r.Outcome);
        Assert.Null(r.Message);
    }

    [Theory]
    [InlineData(null, "a@b.co", "hi there")]
    [InlineData("Sam", null, "hi there")]
    [InlineData("Sam", "a@b.co", null)]
    [InlineData("   ", "a@b.co", "hi there")]
    public void Missing_required_fields_rejected(string? name, string? email, string? message)
    {
        var r = ContactValidator.Validate(Req(name, email, message), "x");
        Assert.Equal(ContactOutcome.Invalid, r.Outcome);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("@no-local.com")]
    [InlineData("spaces in@email.com")]
    public void Bad_email_rejected(string bad)
    {
        var r = ContactValidator.Validate(Req(email: bad), "x");
        Assert.Equal(ContactOutcome.Invalid, r.Outcome);
    }

    [Fact]
    public void Oversized_message_rejected()
    {
        var r = ContactValidator.Validate(Req(message: new string('x', ContactValidator.MessageMax + 1)), "x");
        Assert.Equal(ContactOutcome.Invalid, r.Outcome);
    }

    [Fact]
    public void Html_in_fields_is_encoded()
    {
        var r = ContactValidator.Validate(Req(name: "<script>alert(1)</script>"), "x");

        Assert.Equal(ContactOutcome.Valid, r.Outcome);
        Assert.DoesNotContain("<script>", r.Message!.Name);
        Assert.Contains("&lt;script&gt;", r.Message.Name);
    }
}
