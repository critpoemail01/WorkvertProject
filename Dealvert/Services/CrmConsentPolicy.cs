using Dealvert.Models;

namespace Dealvert.Services;

public static class CrmConsentPolicy
{
    public const string Unknown = "Unknown";
    public const string Consented = "Consented";
    public const string Unsubscribed = "Unsubscribed";
    public const string Suppressed = "Suppressed";

    public static readonly string[] Statuses = [Unknown, Consented, Unsubscribed, Suppressed];

    public static bool CanUseForCampaign(CrmLead lead)
    {
        return !string.IsNullOrWhiteSpace(lead.Email) &&
            lead.Status == "Imported" &&
            CanUseForCampaign(lead.ConsentStatus);
    }

    public static bool CanUseForCampaign(string? consentStatus)
    {
        return !IsBlocked(consentStatus);
    }

    public static bool IsBlocked(string? consentStatus)
    {
        return string.Equals(consentStatus, Unsubscribed, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(consentStatus, Suppressed, StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string? consentStatus)
    {
        if (string.IsNullOrWhiteSpace(consentStatus))
            return Unknown;

        return Statuses.FirstOrDefault(status => status.Equals(consentStatus.Trim(), StringComparison.OrdinalIgnoreCase)) ?? Unknown;
    }
}
