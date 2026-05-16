using Workvert.Models;
using Workvert.Services;
using Microsoft.Extensions.Options;

namespace Workvert.Tests;

public class IntegrationAuthorizationServiceTests
{
    [Fact]
    public void GetPostAuthorization_AllowsOnlyAuthorizedLinkedInOrganization()
    {
        var service = new IntegrationAuthorizationService(new StaticOptionsMonitor<NotificationOptions>(new NotificationOptions()));

        var blocked = service.GetPostAuthorization(new UserNotificationSettings(), "LinkedIn");
        var ready = service.GetPostAuthorization(new UserNotificationSettings
        {
            LinkedInAuthorized = true,
            LinkedInOrganizationName = "Workvert Company Page"
        }, "LinkedIn");

        Assert.False(blocked.IsAuthorized);
        Assert.True(ready.IsAuthorized);
        Assert.Equal("LinkedIn Company Page", ready.Channel);
    }

    [Fact]
    public void GetPostAuthorization_BlocksUnsupportedSocialNetworksInMvp()
    {
        var service = new IntegrationAuthorizationService(new StaticOptionsMonitor<NotificationOptions>(new NotificationOptions()));

        var result = service.GetPostAuthorization(new UserNotificationSettings(), "TikTok");

        Assert.False(result.IsAuthorized);
        Assert.Contains("No official publishing integration", result.Detail);
    }

    [Fact]
    public void GetEmailAuthorization_RequiresSenderProfiles()
    {
        var service = new IntegrationAuthorizationService(new StaticOptionsMonitor<NotificationOptions>(new NotificationOptions
        {
            Email = new EmailNotificationOptions
            {
                SenderProfiles =
                [
                    new EmailSenderProfileOptions
                    {
                        FromName = "Workvert",
                        FromEmail = "growth@example.com",
                        Username = "growth@example.com",
                        Password = "secret",
                        SmtpServer = "smtp.example.com",
                        SmtpPort = 587,
                        EnableSsl = true
                    }
                ]
            }
        }));

        var result = service.GetEmailAuthorization(new UserNotificationSettings { EmailEnabled = true, EmailProvider = "SendGrid" });

        Assert.True(result.IsAuthorized);
        Assert.Equal("SendGrid", result.Channel);
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public StaticOptionsMonitor(T value)
        {
            CurrentValue = value;
        }

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
