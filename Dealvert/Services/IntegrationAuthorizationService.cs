using Dealvert.Models;
using Microsoft.Extensions.Options;

namespace Dealvert.Services;

public sealed class IntegrationAuthorizationService : IIntegrationAuthorizationService
{
    private readonly IOptionsMonitor<NotificationOptions> _options;

    public IntegrationAuthorizationService(IOptionsMonitor<NotificationOptions> options)
    {
        _options = options;
    }

    public PublicationAuthorization GetPostAuthorization(UserNotificationSettings? settings, string platform)
    {
        var normalized = NormalizePlatform(platform);
        return normalized switch
        {
            "linkedin" => Authorized(
                settings?.LinkedInAuthorized == true,
                "LinkedIn Company Page",
                settings?.LinkedInOrganizationName,
                "Requires official OAuth with w_organization_social and an authorized Page role."),
            "instagram" => Authorized(
                settings?.InstagramAuthorized == true,
                "Instagram Business",
                settings?.InstagramBusinessAccountName,
                "Requires official Meta OAuth and an Instagram Business or Creator publishing account."),
            "facebook" => Authorized(
                settings?.FacebookAuthorized == true,
                "Facebook Page",
                settings?.FacebookPageName,
                "Requires official Meta OAuth for an authorized Facebook Page."),
            "google business" => Authorized(
                settings?.GoogleBusinessAuthorized == true,
                "Google Business Profile",
                settings?.GoogleBusinessProfileName,
                "Requires an authorized Google Business Profile connection."),
            _ => new PublicationAuthorization(false, platform, "No official publishing integration is enabled for this channel in the MVP.")
        };
    }

    public PublicationAuthorization GetEmailAuthorization(UserNotificationSettings? settings)
    {
        var senderProfiles = EmailSenderPool.GetConfiguredSenders(_options.CurrentValue.Email);
        var provider = string.IsNullOrWhiteSpace(settings?.EmailProvider)
            ? "Verified email provider"
            : settings!.EmailProvider!;
        var ready = settings?.EmailEnabled != false && senderProfiles.Count > 0;
        return new PublicationAuthorization(
            ready,
            provider,
            ready
                ? $"{senderProfiles.Count} verified sender profile{(senderProfiles.Count == 1 ? "" : "s")} available."
                : "Email requires an authorized sender provider such as Brevo, Mailchimp, SendGrid, Amazon SES or verified SMTP profiles.");
    }

    private static PublicationAuthorization Authorized(bool authorized, string channel, string? accountName, string missingDetail)
    {
        return new PublicationAuthorization(
            authorized,
            channel,
            authorized && !string.IsNullOrWhiteSpace(accountName)
                ? $"Authorized account: {accountName}."
                : missingDetail);
    }

    private static string NormalizePlatform(string platform)
    {
        var value = (platform ?? string.Empty).Trim().ToLowerInvariant();
        if (value.Contains("linkedin", StringComparison.Ordinal)) return "linkedin";
        if (value.Contains("instagram", StringComparison.Ordinal)) return "instagram";
        if (value.Contains("facebook", StringComparison.Ordinal)) return "facebook";
        if (value.Contains("google", StringComparison.Ordinal)) return "google business";
        return value;
    }
}
