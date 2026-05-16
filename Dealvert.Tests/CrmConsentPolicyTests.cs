using Dealvert.Models;
using Dealvert.Services;

namespace Dealvert.Tests;

public class CrmConsentPolicyTests
{
    [Theory]
    [InlineData("Unknown", true)]
    [InlineData("Consented", true)]
    [InlineData("Unsubscribed", false)]
    [InlineData("Suppressed", false)]
    public void CanUseForCampaign_BlocksSuppressionStatuses(string consentStatus, bool expected)
    {
        var lead = new CrmLead
        {
            Email = "ana@example.com",
            Status = "Imported",
            ConsentStatus = consentStatus
        };

        Assert.Equal(expected, CrmConsentPolicy.CanUseForCampaign(lead));
    }

    [Fact]
    public void Normalize_FallsBackToUnknownForUnsupportedValues()
    {
        Assert.Equal(CrmConsentPolicy.Unknown, CrmConsentPolicy.Normalize("manual"));
        Assert.Equal(CrmConsentPolicy.Consented, CrmConsentPolicy.Normalize(" consented "));
    }
}
