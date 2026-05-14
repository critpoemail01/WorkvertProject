using Alivert.Models;

namespace Alivert.Services;

public sealed class TemplateAiMarketingPlannerService : IAiMarketingPlannerService
{
    private static readonly Dictionary<string, PlatformProfile> PlatformProfiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TikTok"] = new("TikTok", "short video", "fast hook, visual demo, human tone", "#startup #productivity #saas #founders", 4200, 0.09m, 0.018m),
        ["Instagram"] = new("Instagram", "reel or carousel", "visual story, concise caption, proof point", "#app #digitalbusiness #growth #marketing", 3200, 0.075m, 0.014m),
        ["Facebook"] = new("Facebook", "community post", "problem-led copy, social proof, clear question", "#smallbusiness #onlinebusiness #growth", 2100, 0.052m, 0.010m),
        ["LinkedIn"] = new("LinkedIn", "B2B post", "business context, measurable outcome, professional CTA", "#b2b #saas #growth #automation", 1800, 0.061m, 0.018m),
        ["X"] = new("X", "short thread", "sharp insight, punchy wording, one clear takeaway", "#buildinpublic #saas #growth", 1400, 0.047m, 0.009m),
        ["YouTube Shorts"] = new("YouTube Shorts", "short video", "demo-first script, title promise, retention beat", "#shorts #appdemo #saas", 3600, 0.063m, 0.013m)
    };

    private static readonly string[] Angles =
    {
        "pain point",
        "before and after",
        "product demo",
        "customer objection",
        "founder story",
        "use case",
        "comparison",
        "quick win",
        "social proof",
        "limited offer"
    };

    public AiMarketingPlanDraft Generate(AiMarketingPlanRequest request)
    {
        var schedule = BuildSchedule(request.StartDate, request.EndDate, request.Frequency).ToList();
        var platforms = request.Platforms.Count == 0
            ? new List<string> { "TikTok", "Instagram", "Facebook", "LinkedIn" }
            : request.Platforms.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var posts = new List<MarketingPostSuggestion>();
        var dayIndex = 0;

        foreach (var date in schedule)
        {
            dayIndex++;
            foreach (var platform in platforms)
            {
                var profile = PlatformProfiles.TryGetValue(platform, out var configured)
                    ? configured
                    : new PlatformProfile(platform, "social post", "clear hook, proof, CTA", "#growth #marketing", 1200, 0.045m, 0.008m);

                var angle = Angles[(dayIndex + platform.Length) % Angles.Length];
                var scheduledAt = date.ToDateTime(new TimeOnly(DefaultHourFor(profile.Name), 0), DateTimeKind.Utc);
                var reach = ScaleReach(profile.BaseReach + (dayIndex * 37) + (request.ProductName.Length * 11), request.Location);
                var interactions = Math.Max(1, (int)Math.Round(reach * profile.InteractionRate));
                var conversions = Math.Max(0, (int)Math.Round(interactions * profile.ConversionRate));

                posts.Add(new MarketingPostSuggestion
                {
                    Platform = Clip(profile.Name, 40),
                    ScheduledForUtc = scheduledAt,
                    DayNumber = dayIndex,
                    Title = Clip($"{request.ProductName}: {TitleForAngle(angle)}", 140),
                    Hook = Clip(HookFor(profile, request, angle), 300),
                    Caption = Clip(CaptionFor(profile, request, angle), 1600),
                    CreativeBrief = Clip(CreativeBriefFor(profile, request, angle), 900),
                    Hashtags = Clip(profile.Hashtags, 300),
                    CallToAction = Clip(CallToActionFor(request), 180),
                    Status = "Draft",
                    EstimatedReach = reach,
                    EstimatedInteractions = interactions,
                    EstimatedConversions = conversions
                });
            }
        }

        var emails = BuildEmails(request, schedule);
        var leads = BuildLeadSuggestions(request);

        return new AiMarketingPlanDraft(posts, emails, leads);
    }

    private static IEnumerable<DateOnly> BuildSchedule(DateOnly startDate, DateOnly endDate, string frequency)
    {
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var day = date.DayOfWeek;
            var include = frequency switch
            {
                "Weekdays" => day is not DayOfWeek.Saturday and not DayOfWeek.Sunday,
                "ThreePerWeek" => day is DayOfWeek.Monday or DayOfWeek.Wednesday or DayOfWeek.Friday,
                "Weekly" => date == startDate || day == startDate.DayOfWeek,
                _ => true
            };

            if (include)
                yield return date;
        }
    }

    private static List<MarketingEmailSuggestion> BuildEmails(AiMarketingPlanRequest request, IReadOnlyList<DateOnly> schedule)
    {
        if (schedule.Count == 0)
            return new List<MarketingEmailSuggestion>();

        var offsets = new[] { 0, 3, 7, 14, 21, 28 };
        var themes = new[]
        {
            ("Quick intro", "A short, direct opener that explains the problem and why the product exists."),
            ("Pain and cost", "Show the cost of doing nothing, then position the product as the lower-friction path."),
            ("Use case", "Explain one concrete workflow for the target audience."),
            ("Proof and trust", "Show proof, expected outcome and a practical next step."),
            ("Offer", "Make the offer clear and reduce risk."),
            ("Final follow-up", "Short reminder with a human close and one CTA.")
        };

        var emails = new List<MarketingEmailSuggestion>();
        for (var i = 0; i < offsets.Length; i++)
        {
            var date = request.StartDate.AddDays(offsets[i]);
            if (date > request.EndDate)
                continue;

            var theme = themes[i];
                var reach = Math.Max(1, CountAudienceContacts(request.EmailAudience));
                if (reach == 1)
                reach = ScaleReach(80 + (i * 16), request.Location);

            emails.Add(new MarketingEmailSuggestion
            {
                ScheduledForUtc = date.ToDateTime(new TimeOnly(9, 30), DateTimeKind.Utc),
                DayNumber = offsets[i] + 1,
                Subject = Clip(SubjectFor(request, theme.Item1), 160),
                PreviewText = Clip(theme.Item2, 220),
                Body = Clip(EmailBodyFor(request, theme.Item1), 4000),
                AudienceSegment = Clip(string.IsNullOrWhiteSpace(request.EmailAudience) ? "Suggested outbound audience" : "Provided potential-client list", 160),
                Status = "Draft",
                EstimatedReach = reach,
                EstimatedInteractions = Math.Max(1, (int)Math.Round(reach * 0.18m)),
                EstimatedConversions = Math.Max(0, (int)Math.Round(reach * 0.035m))
            });
        }

        return emails;
    }

    private static List<MarketingLeadSuggestion> BuildLeadSuggestions(AiMarketingPlanRequest request)
    {
        var audience = request.TargetAudience.ToLowerInvariant();
        string[] industries = audience.Contains("saas") || audience.Contains("b2b")
            ? new[] { "B2B SaaS", "IT services", "Business operations", "Digital agencies", "Consulting" }
            : audience.Contains("restaurant") || audience.Contains("food")
                ? new[] { "Restaurants", "Hospitality groups", "Food delivery brands", "Local franchises", "Event venues" }
                : new[] { "Online services", "Small businesses", "Digital commerce", "Professional services", "Education providers" };

        var roles = new[] { "Founder", "Growth lead", "Marketing manager", "Operations manager", "Sales lead" };
        var leads = new List<MarketingLeadSuggestion>();

        for (var i = 0; i < industries.Length; i++)
        {
            leads.Add(new MarketingLeadSuggestion
            {
                CompanyProfile = Clip($"{industries[i]} company with visible online acquisition needs", 160),
                Industry = Clip(industries[i], 120),
                ContactRole = Clip(roles[i % roles.Length], 120),
                EmailSearchHint = Clip($"Search '{industries[i]} {roles[i % roles.Length]} email' or LinkedIn company pages", 180),
                Reason = Clip($"{request.ProductName} can be positioned around '{request.ValueProposition}' for this segment in {request.Location.Summary}.", 500),
                Status = "Suggested"
            });
        }

        return leads;
    }

    private static string TitleForAngle(string angle)
    {
        return angle switch
        {
            "pain point" => "solve the daily friction",
            "before and after" => "from messy workflow to measurable result",
            "product demo" => "show the product in action",
            "customer objection" => "answer the biggest buying doubt",
            "founder story" => "why this should exist now",
            "use case" => "one workflow worth copying",
            "comparison" => "compare the old way with the new way",
            "quick win" => "one practical result today",
            "social proof" => "make the outcome believable",
            _ => "make the offer clear"
        };
    }

    private static string HookFor(PlatformProfile profile, AiMarketingPlanRequest request, string angle)
    {
        var location = LocationPhrase(request.Location);
        return profile.Name switch
        {
            "TikTok" => $"Most {request.TargetAudience}{location} lose time here: {request.ValueProposition}.",
            "Instagram" => $"A simple way to make {request.CampaignGoal.ToLowerInvariant()} feel less random{location}.",
            "Facebook" => $"Question for {request.TargetAudience}{location}: what would change if this workflow was automatic?",
            "LinkedIn" => $"{request.ProductName} turns a common {request.TargetAudience} problem{location} into a measurable workflow.",
            "X" => $"The underrated growth move: make {request.ValueProposition.ToLowerInvariant()} obvious.",
            "YouTube Shorts" => $"Watch {request.ProductName} solve this in under 30 seconds.",
            _ => $"{request.ProductName}: {TitleForAngle(angle)}."
        };
    }

    private static string CaptionFor(PlatformProfile profile, AiMarketingPlanRequest request, string angle)
    {
        var urlLine = string.IsNullOrWhiteSpace(request.ProductUrl) ? "" : $"\n\nTry it: {request.ProductUrl}";
        var locationLine = LocationSentence(request.Location);
        return profile.Name switch
        {
            "LinkedIn" =>
                $"{request.TargetAudience} do not need more busywork. They need a repeatable way to get {request.CampaignGoal.ToLowerInvariant()}.{locationLine}\n\n{request.ProductName} focuses on {request.ValueProposition}.\n\nThe angle for today: {TitleForAngle(angle)}.{urlLine}",
            "TikTok" =>
                $"Hook the problem in the first 2 seconds, show {request.ProductName}, then make the outcome concrete for {request.Location.Summary}: {request.ValueProposition}.{urlLine}",
            "Instagram" =>
                $"Turn this into a {profile.Format}: problem, product moment, result, CTA. {request.ProductName} helps {request.TargetAudience} get {request.CampaignGoal.ToLowerInvariant()} in {request.Location.Summary}.{urlLine}",
            "Facebook" =>
                $"{request.TargetAudience}{LocationPhrase(request.Location)} often know the problem, but delay fixing it. Position {request.ProductName} as the practical next step: {request.ValueProposition}.{urlLine}",
            _ =>
                $"{request.ProductName} helps {request.TargetAudience} in {request.Location.Summary} with {request.ValueProposition}. Focus this post on {TitleForAngle(angle)}.{urlLine}"
        };
    }

    private static string CreativeBriefFor(PlatformProfile profile, AiMarketingPlanRequest request, string angle)
    {
        return $"Format: {profile.Format}. Style: {profile.Style}. Show the product context, the audience problem in {request.Location.Summary}, and one measurable next step. Angle: {TitleForAngle(angle)}. Tone: {request.Tone}.";
    }

    private static string CallToActionFor(AiMarketingPlanRequest request)
    {
        return string.IsNullOrWhiteSpace(request.ProductUrl)
            ? $"Ask for a demo of {request.ProductName}"
            : $"Visit {request.ProductUrl}";
    }

    private static string SubjectFor(AiMarketingPlanRequest request, string theme)
    {
        return theme switch
        {
            "Quick intro" => $"{request.ProductName} for {request.TargetAudience}",
            "Pain and cost" => $"Is this slowing down your {request.CampaignGoal.ToLowerInvariant()}?",
            "Use case" => $"A practical workflow using {request.ProductName}",
            "Proof and trust" => $"Why teams choose {request.ProductName}",
            "Offer" => $"A simpler way to start with {request.ProductName}",
            _ => $"Should I close the loop on {request.ProductName}?"
        };
    }

    private static string EmailBodyFor(AiMarketingPlanRequest request, string theme)
    {
        var cta = string.IsNullOrWhiteSpace(request.ProductUrl)
            ? "Would it make sense to send you a quick walkthrough?"
            : $"You can review it here: {request.ProductUrl}";

        return theme switch
        {
            "Quick intro" =>
                $"Hi {{FirstName}},\n\nI noticed teams like yours in {request.Location.Summary} often need a clearer way to reach {request.CampaignGoal.ToLowerInvariant()}.\n\n{request.ProductName} helps {request.TargetAudience} with {request.ValueProposition}.\n\n{cta}",
            "Pain and cost" =>
                $"Hi {{FirstName}},\n\nThe expensive part is not just the task itself. It is the delay, manual follow-up and missed opportunities around it.\n\nThat is where {request.ProductName} can help: {request.ValueProposition}.\n\n{cta}",
            "Use case" =>
                $"Hi {{FirstName}},\n\nOne practical use case: take a recurring acquisition workflow, define the audience and region, and let {request.ProductName} keep the next action moving.\n\nFor {request.TargetAudience} in {request.Location.Summary}, that means a clearer path to {request.CampaignGoal.ToLowerInvariant()}.\n\n{cta}",
            "Proof and trust" =>
                $"Hi {{FirstName}},\n\nThe goal is not more tools. It is a repeatable process that can be measured.\n\n{request.ProductName} keeps the focus on {request.CampaignGoal.ToLowerInvariant()}, with messaging built around {request.ValueProposition}.\n\n{cta}",
            "Offer" =>
                $"Hi {{FirstName}},\n\nIf this is relevant, the easiest next step is to test {request.ProductName} with one specific campaign or workflow.\n\nYou will quickly see whether {request.ValueProposition} fits your team.\n\n{cta}",
            _ =>
                $"Hi {{FirstName}},\n\nI do not want to keep chasing if this is not a priority.\n\nShould I close the loop, or is {request.ProductName} worth a quick look for {request.TargetAudience}?\n\n{cta}"
        };
    }

    private static int DefaultHourFor(string platform)
    {
        return platform switch
        {
            "LinkedIn" => 8,
            "TikTok" => 18,
            "Instagram" => 17,
            "Facebook" => 12,
            "YouTube Shorts" => 19,
            _ => 10
        };
    }

    private static int CountAudienceContacts(string? audience)
    {
        if (string.IsNullOrWhiteSpace(audience))
            return 0;

        return audience
            .Split(new[] { '\r', '\n', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Count(contact => !string.IsNullOrWhiteSpace(contact));
    }

    private static int ScaleReach(int reach, AiAudienceLocation location)
    {
        var multiplier = location.Scope switch
        {
            "City" => 0.58m,
            "Country" => 0.82m,
            _ => 1.0m
        };

        return Math.Max(1, (int)Math.Round(reach * multiplier));
    }

    private static string LocationPhrase(AiAudienceLocation location)
    {
        return location.Scope.Equals("World", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : $" in {location.Summary}";
    }

    private static string LocationSentence(AiAudienceLocation location)
    {
        return location.Scope.Equals("World", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : $" Focus the message on {location.Summary}.";
    }

    private static string Clip(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;

        return value[..Math.Max(0, maxLength - 3)].TrimEnd() + "...";
    }

    private sealed record PlatformProfile(
        string Name,
        string Format,
        string Style,
        string Hashtags,
        int BaseReach,
        decimal InteractionRate,
        decimal ConversionRate);
}
