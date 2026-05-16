using Dealvert.Services;

namespace Dealvert.Tests;

public class EmailSenderPoolTests
{
    [Fact]
    public void GetConfiguredSenders_UsesLegacySenderWhenProfilesAreEmpty()
    {
        var senders = EmailSenderPool.GetConfiguredSenders(new EmailNotificationOptions
        {
            FromName = "Dealvert",
            FromEmail = "growth@example.com",
            Username = "",
            Password = "secret",
            SmtpServer = "smtp.example.com",
            SmtpPort = 587,
            EnableSsl = true
        });

        var sender = Assert.Single(senders);
        Assert.Equal("growth@example.com", sender.FromEmail);
        Assert.Equal("growth@example.com", sender.Username);
        Assert.Equal("smtp.example.com", sender.SmtpServer);
    }

    [Fact]
    public void GetConfiguredSenders_ReturnsOnlyValidVerifiedProfiles()
    {
        var senders = EmailSenderPool.GetConfiguredSenders(new EmailNotificationOptions
        {
            Password = "fallback-secret",
            SmtpServer = "smtp.example.com",
            SmtpPort = 587,
            SenderProfiles =
            [
                new EmailSenderProfileOptions
                {
                    Name = "Founder",
                    FromName = "Founder",
                    FromEmail = "founder@example.com"
                },
                new EmailSenderProfileOptions
                {
                    Name = "Missing address"
                }
            ]
        });

        var sender = Assert.Single(senders);
        Assert.Equal("Founder", sender.Name);
        Assert.Equal("Founder", sender.FromName);
        Assert.Equal("founder@example.com", sender.FromEmail);
        Assert.Equal("fallback-secret", sender.Password);
    }

    [Fact]
    public void SelectSender_DistributesRecipientsAcrossProfiles()
    {
        var senders = new[]
        {
            Sender("growth@example.com"),
            Sender("founder@example.com"),
            Sender("sales@example.com")
        };

        var selected = Enumerable.Range(0, 30)
            .Select(i => EmailSenderPool.SelectSender(senders, 42, $"lead{i}@example.com", i).FromEmail)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        Assert.True(selected > 1);
    }

    private static EmailSenderProfile Sender(string email)
    {
        return new EmailSenderProfile(email, "Dealvert", email, email, "secret", "smtp.example.com", 587, true);
    }
}
