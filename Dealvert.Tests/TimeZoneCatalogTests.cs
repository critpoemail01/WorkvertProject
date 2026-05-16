using Dealvert.Services;

namespace Dealvert.Tests;

public class TimeZoneCatalogTests
{
    [Fact]
    public void Normalize_ReturnsCanonicalIanaIds()
    {
        Assert.Equal("Europe/Lisbon", TimeZoneCatalog.Normalize("GMT Standard Time"));
        Assert.Equal("America/New_York", TimeZoneCatalog.Normalize("Eastern Standard Time"));
        Assert.Equal(TimeZoneCatalog.DefaultTimeZoneId, TimeZoneCatalog.Normalize("Not/AZone"));
    }

    [Fact]
    public void SupportedScheduleTimeZones_MapToSystemTimeZones()
    {
        foreach (var timeZoneId in TimeZoneCatalog.SupportedTimeZoneIds)
        {
            var systemId = TimeZoneCatalog.ToSystemTimeZoneId(timeZoneId);
            var zone = TimeZoneInfo.FindSystemTimeZoneById(systemId);

            Assert.False(string.IsNullOrWhiteSpace(zone.Id));
        }
    }
}
