namespace Workvert.Services;

public sealed record ScheduleTimeZoneChoice(string Id, string Label);

public static class TimeZoneCatalog
{
    public const string DefaultTimeZoneId = "Europe/Lisbon";

    private static readonly ScheduleTimeZoneDefinition[] Definitions =
    [
        new("Europe/Lisbon", "Lisbon / London trading day", "GMT Standard Time"),
        new("UTC", "UTC / exchange server time", "UTC"),
        new("Europe/Madrid", "Madrid / Paris market hours", "Romance Standard Time"),
        new("Europe/Berlin", "Berlin / Rome / Amsterdam", "W. Europe Standard Time"),
        new("Europe/Athens", "Athens / Bucharest", "GTB Standard Time"),
        new("America/New_York", "New York / Toronto", "Eastern Standard Time"),
        new("America/Chicago", "Chicago", "Central Standard Time"),
        new("America/Denver", "Denver", "Mountain Standard Time"),
        new("America/Los_Angeles", "Los Angeles / Vancouver", "Pacific Standard Time"),
        new("America/Mexico_City", "Mexico City", "Central Standard Time (Mexico)"),
        new("America/Sao_Paulo", "Sao Paulo", "E. South America Standard Time"),
        new("America/Buenos_Aires", "Buenos Aires", "Argentina Standard Time"),
        new("Asia/Dubai", "Dubai / Abu Dhabi", "Arabian Standard Time"),
        new("Asia/Kolkata", "India", "India Standard Time"),
        new("Asia/Singapore", "Singapore / Kuala Lumpur", "Singapore Standard Time"),
        new("Asia/Hong_Kong", "Hong Kong / Beijing", "China Standard Time"),
        new("Asia/Tokyo", "Tokyo", "Tokyo Standard Time"),
        new("Australia/Sydney", "Sydney / Melbourne", "AUS Eastern Standard Time")
    ];

    public static IReadOnlyList<ScheduleTimeZoneChoice> GetScheduleChoices(DateTime utcNow)
    {
        return Definitions
            .Select(zone => new ScheduleTimeZoneChoice(zone.IanaId, $"{zone.IanaId} - {zone.Label} ({FormatOffset(zone, utcNow)})"))
            .ToArray();
    }

    public static IReadOnlyList<string> SupportedTimeZoneIds => Definitions.Select(zone => zone.IanaId).ToArray();

    public static string Normalize(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return DefaultTimeZoneId;

        var trimmed = timeZoneId.Trim();
        var match = Definitions.FirstOrDefault(zone =>
            string.Equals(zone.IanaId, trimmed, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(zone.WindowsId, trimmed, StringComparison.OrdinalIgnoreCase));

        return match?.IanaId ?? DefaultTimeZoneId;
    }

    public static string ToSystemTimeZoneId(string? timeZoneId)
    {
        var normalized = Normalize(timeZoneId);
        var match = Definitions.FirstOrDefault(zone => string.Equals(zone.IanaId, normalized, StringComparison.OrdinalIgnoreCase));
        if (match is null)
            return OperatingSystem.IsWindows() ? "GMT Standard Time" : DefaultTimeZoneId;

        return OperatingSystem.IsWindows() ? match.WindowsId : match.IanaId;
    }

    private static string FormatOffset(ScheduleTimeZoneDefinition zone, DateTime utcNow)
    {
        try
        {
            var info = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? zone.WindowsId : zone.IanaId);
            return FormatOffset(info.GetUtcOffset(utcNow));
        }
        catch (TimeZoneNotFoundException)
        {
            return "UTC+00:00";
        }
        catch (InvalidTimeZoneException)
        {
            return "UTC+00:00";
        }
    }

    private static string FormatOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var absolute = offset.Duration();
        return $"UTC{sign}{absolute:hh\\:mm}";
    }

    private sealed record ScheduleTimeZoneDefinition(string IanaId, string Label, string WindowsId);
}
