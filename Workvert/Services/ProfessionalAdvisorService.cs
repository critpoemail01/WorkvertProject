using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Workvert.Services;

public sealed class ProfessionalAdvisorService : IProfessionalAdvisorService
{
    private static readonly IReadOnlyDictionary<string, string[]> ProfessionSkills = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["programador"] = ["C#", ".NET", "SQL Server", "APIs", "Git", "Azure", "Docker", "Automated testing", "Software architecture"],
        ["developer"] = ["C#", ".NET", "SQL Server", "APIs", "Git", "Azure", "Docker", "Automated testing", "Software architecture"],
        ["software developer"] = ["C#", ".NET", "SQL Server", "APIs", "Git", "Azure", "Docker", "Automated testing", "Software architecture"],
        ["designer"] = ["Figma", "Photoshop", "Illustrator", "Branding", "UI/UX", "Prototyping", "Social media"],
        ["eletricista"] = ["Electrical installations", "Electrical panels", "Diagnostics", "Safety", "Quoting"],
        ["electrician"] = ["Electrical installations", "Electrical panels", "Diagnostics", "Safety", "Quoting"],
        ["contabilista"] = ["Accounting", "Tax", "Excel", "ERP", "Bank reconciliation", "Reporting"],
        ["accountant"] = ["Accounting", "Tax", "Excel", "ERP", "Bank reconciliation", "Reporting"],
        ["gestor comercial"] = ["CRM", "Prospecting", "Negotiation", "Pipeline", "Excel", "Sales reporting"],
        ["sales manager"] = ["CRM", "Prospecting", "Negotiation", "Pipeline", "Excel", "Sales reporting"],
        ["tecnico manutencao"] = ["Diagnostics", "Preventive maintenance", "Safety", "Technical reporting", "Customer service"],
        ["maintenance technician"] = ["Diagnostics", "Preventive maintenance", "Safety", "Technical reporting", "Customer service"],
        ["engenheiro"] = ["Project management", "AutoCAD", "Technical analysis", "Quoting", "Technical documentation"],
        ["engineer"] = ["Project management", "AutoCAD", "Technical analysis", "Quoting", "Technical documentation"]
    };

    private static readonly IReadOnlyList<OpportunityTemplate> Opportunities =
    [
        new(".NET Developer", "B2B SaaS team", "Full-time job", "Portugal", "Remote",
            ["c#", ".net", "sql server", "apis", "git"], ["azure", "docker", "automated testing"], "EUR 32k-55k/year"),
        new("Full-stack Developer for internal product", "Industrial company", "Full-time job", "Lisbon", "Hybrid",
            ["c#", ".net", "sql server", "javascript", "apis"], ["blazor", "azure"], "EUR 28k-45k/year"),
        new("UI/UX Designer", "Digital agency", "Full-time job", "Portugal", "Remote",
            ["figma", "ui/ux", "prototyping", "design"], ["design system", "research"], "EUR 22k-38k/year"),
        new("Branding and social media specialist", "Growing SMB", "Freelance", "Portugal", "Remote",
            ["photoshop", "illustrator", "branding", "social media"], ["copywriting", "short-form video"], "EUR 350-1200/project"),
        new("Certified electrician for small jobs", "Local clients", "Service work", "Preferred city", "On-site",
            ["electrical installations", "diagnostics", "safety"], ["certification", "quoting"], "EUR 25-45/hour"),
        new("Maintenance technician", "Facilities operator", "Full-time job", "Portugal", "On-site",
            ["diagnostics", "preventive maintenance", "technical reporting"], ["electricity", "hvac"], "EUR 18k-30k/year"),
        new("Monthly accounting support", "Microbusinesses", "Freelance", "Portugal", "Hybrid",
            ["accounting", "tax", "excel"], ["erp", "reporting"], "EUR 120-450/client/month"),
        new("B2B sales manager", "Services company", "Full-time job", "Portugal", "Hybrid",
            ["crm", "prospecting", "negotiation", "pipeline"], ["linkedin sales navigator", "reporting"], "EUR 24k-42k/year + commission"),
        new("3D modeler / technical drafting", "Architecture studio", "Freelance", "International", "Remote",
            ["autocad", "revit", "sketchup", "solidworks"], ["rendering", "bim"], "EUR 20-55/hour"),
        new("SMB automations and integrations", "Independent clients", "Freelance", "International", "Remote",
            ["apis", "sql server", "automations", "c#", ".net"], ["power automate", "documentation"], "EUR 500-3500/project")
    ];

    private static readonly IReadOnlyList<ProfessionalTemplate> Professionals =
    [
        new("Ana Martins", "Web developer", "Porto", "2 weeks", "EUR 35/hour", ["web", "c#", ".net", "seo", "hosting"]),
        new("Joao Silva", "Certified electrician", "Lisbon", "48 hours", "EUR 32/hour", ["electricity", "installations", "safety", "diagnostics"]),
        new("Marta Costa", "UI/UX designer", "Remote", "1 week", "EUR 30/hour", ["figma", "branding", "ui/ux", "landing pages"]),
        new("Rui Ferreira", "AutoCAD/Revit specialist", "Braga", "3 days", "EUR 28/hour", ["autocad", "revit", "3d modeling", "technical drafting"]),
        new("Clara Sousa", "Accountant", "Coimbra", "1 week", "EUR 180/month", ["accounting", "tax", "excel", "erp"]),
        new("Tiago Lopes", "Automation consultant", "Remote", "1 week", "EUR 45/hour", ["apis", "automations", "sql server", "integrations"])
    ];

    public ProfessionalProfileAnalysis AnalyzeProfile(ProfessionalProfileRequest request)
    {
        var profession = request.CurrentProfession.Trim();
        var userTerms = BuildTermSet(
            request.CurrentProfession,
            request.Experience,
            request.TechnicalSkills,
            request.SoftSkills,
            request.Tools,
            request.Education,
            request.Languages,
            request.InterestAreas);

        var suggestedSkills = SuggestSkills(profession, userTerms);
        var jobOpportunities = Opportunities
            .Where(x => !IsFreelanceOrService(x.Type))
            .Select(x => BuildOpportunityRecommendation(x, request, userTerms))
            .Where(x => x.CompatibilityScore >= 45)
            .OrderByDescending(x => x.CompatibilityScore)
            .ThenBy(x => x.Title)
            .Take(6)
            .ToList();

        var freelanceOpportunities = Opportunities
            .Where(x => IsFreelanceOrService(x.Type))
            .Select(x => BuildFreelanceRecommendation(x, request, userTerms))
            .Where(x => x.CompatibilityScore >= 40)
            .OrderByDescending(x => x.CompatibilityScore)
            .ThenBy(x => x.Service)
            .Take(6)
            .ToList();

        var allMissingSkills = jobOpportunities
            .SelectMany(x => x.MissingSkills)
            .Concat(freelanceOpportunities.SelectMany(x => x.RequiredSkills.Where(skill => !ContainsTerm(userTerms, skill))))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

        return new ProfessionalProfileAnalysis(
            suggestedSkills,
            jobOpportunities,
            freelanceOpportunities,
            BuildCareerPlan(request, suggestedSkills, allMissingSkills),
            BuildGeneratedAssets(request, suggestedSkills),
            "Profile photos are used only for presentation, a visual CV or a professional avatar. They are not used to calculate compatibility or recommend opportunities.");
    }

    public ServiceRequestAnalysis AnalyzeServiceRequest(ServiceRequestRequest request)
    {
        var terms = BuildTermSet(request.Description, request.Location, request.Budget, request.Urgency);
        var serviceArea = DetectServiceArea(terms);
        var requiredSkills = DetectRequiredSkills(serviceArea, terms);
        var professionalType = DetectProfessionalType(serviceArea);
        var complexity = DetectComplexity(terms, request.Description);
        var deliveryMode = request.RemoteAllowed && serviceArea is not "Electrical work" and not "Maintenance"
            ? "Can be delivered remotely"
            : "Requires on-site presence or local validation";

        var recommended = Professionals
            .Select(x => ScoreProfessional(x, requiredSkills, request, serviceArea))
            .Where(x => x.CompatibilityScore >= 35)
            .OrderByDescending(x => x.CompatibilityScore)
            .Take(5)
            .ToList();

        return new ServiceRequestAnalysis(
            serviceArea,
            professionalType,
            complexity,
            deliveryMode,
            string.IsNullOrWhiteSpace(request.Budget) ? "Budget not provided" : request.Budget.Trim(),
            requiredSkills,
            recommended,
            BuildClientBrief(request, professionalType, requiredSkills, complexity, deliveryMode));
    }

    private static OpportunityRecommendation BuildOpportunityRecommendation(
        OpportunityTemplate template,
        ProfessionalProfileRequest request,
        IReadOnlySet<string> userTerms)
    {
        var matchedSkills = template.RequiredSkills
            .Where(skill => ContainsTerm(userTerms, skill))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var matchedRequired = matchedSkills.Count;
        var missing = template.NiceToHave
            .Where(skill => !ContainsTerm(userTerms, skill))
            .Take(4)
            .ToList();

        var score = 25 + matchedRequired * 10;
        score += LocationScore(template, request);
        score += EngagementScore(template, request);
        score = Math.Clamp(score, 0, 97);

        var reasons = new List<string>
        {
            matchedRequired > 0
                ? $"Matches {matchedRequired} strong profile skill{(matchedRequired == 1 ? string.Empty : "s")}."
                : "Related to the profession or interest areas in the profile.",
            $"Work mode: {template.WorkMode}.",
            $"Expected range: {template.CompensationRange}."
        };

        if (!string.IsNullOrWhiteSpace(request.DesiredLocation))
        {
            reasons.Add($"Preferred location considered: {request.DesiredLocation.Trim()}.");
        }

        return new OpportunityRecommendation(
            template.Title,
            template.Organization,
            template.Type,
            ResolveLocation(template.Location, request.DesiredLocation),
            template.WorkMode,
            score,
            reasons,
            matchedSkills,
            missing,
            missing.Count == 0
                ? "Apply with a tailored CV."
                : $"Prepare the application and strengthen {string.Join(", ", missing.Take(2))}.",
            template.CompensationRange);
    }

    private static FreelanceRecommendation BuildFreelanceRecommendation(
        OpportunityTemplate template,
        ProfessionalProfileRequest request,
        IReadOnlySet<string> userTerms)
    {
        var matchedSkills = template.RequiredSkills
            .Where(skill => ContainsTerm(userTerms, skill))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var missingSkills = template.RequiredSkills
            .Concat(template.NiceToHave)
            .Where(skill => !ContainsTerm(userTerms, skill))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
        var matchedRequired = matchedSkills.Count;
        var score = 20 + matchedRequired * 12 + EngagementScore(template, request);
        score += Normalize(request.EngagementType).Contains("freelance", StringComparison.OrdinalIgnoreCase) ? 12 : 0;
        score = Math.Clamp(score, 0, 96);

        return new FreelanceRecommendation(
            template.Title,
            template.Organization,
            template.CompensationRange,
            score,
            template.RequiredSkills.Concat(template.NiceToHave).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            matchedSkills,
            missingSkills,
            $"Lead with outcomes, timeline, deliverables and relevant work examples from {FirstNonEmpty(request.PortfolioUrl, "your portfolio")}.",
            missingSkills.Count == 0
                ? "Package this as a clear service offer and send a short proposal."
                : $"Create the offer and strengthen {string.Join(", ", missingSkills.Take(2))}.");
    }

    private static CareerPlan BuildCareerPlan(
        ProfessionalProfileRequest request,
        IReadOnlyList<string> suggestedSkills,
        IReadOnlyList<string> allMissingSkills)
    {
        var coreGaps = allMissingSkills.Count > 0 ? allMissingSkills : suggestedSkills.Take(5).ToList();
        var profession = string.IsNullOrWhiteSpace(request.CurrentProfession) ? "the selected professional area" : request.CurrentProfession.Trim();

        return new CareerPlan(
            $"The profile is positioned for opportunities in {profession}, with stronger potential when technical skills, portfolio evidence and business outcomes are combined.",
            coreGaps,
            coreGaps.Select(skill => $"Strengthen {skill} with a demonstrable project or a short certification.").Take(5).ToList(),
            [
                "Turn responsibilities into measurable outcomes.",
                "Add tools, business context and impact to each experience.",
                "Create a short CV version for fast applications.",
                "Keep LinkedIn, portfolio and outreach aligned with the career target."
            ],
            [
                "Confirm the suggested skills and remove anything that does not apply.",
                "Choose 3 priority opportunity types.",
                "Prepare a tailored CV and application message.",
                "Enable alerts for opportunities above 75% compatibility.",
                "Review results weekly and adjust preferences."
            ]);
    }

    private static GeneratedProfileAssets BuildGeneratedAssets(
        ProfessionalProfileRequest request,
        IReadOnlyList<string> suggestedSkills)
    {
        var profession = FirstNonEmpty(request.CurrentProfession, "professional");
        var skills = BuildDisplaySkills(request.TechnicalSkills, suggestedSkills);
        var location = FirstNonEmpty(request.DesiredLocation, "national and international markets");

        return new GeneratedProfileAssets(
            $"{profession} with experience in {skills}. Practical profile focused on solving real problems, communicating clearly and delivering work adapted to client or employer needs.",
            $"{profession} | {skills} | Available for {location}",
            $"I am a {profession} focused on practical work, continuous improvement and measurable results. I am interested in opportunities aligned with {FirstNonEmpty(request.InterestAreas, "my core skills")} and available for {FirstNonEmpty(request.WorkMode, "flexible work models")}.",
            $"Hello, I found this opportunity and believe my profile in {skills} can contribute directly. I can share work examples, explain my experience and adapt the application to the team's goals.",
            $"I can help with services related to {skills}, from diagnosis and planning to execution, documentation and follow-up. I work with clear budgets, defined timelines and regular communication.");
    }

    private static IReadOnlyList<string> SuggestSkills(string profession, IReadOnlySet<string> userTerms)
    {
        var matched = ProfessionSkills
            .Where(x => profession.Contains(x.Key, StringComparison.OrdinalIgnoreCase) || ContainsTerm(userTerms, x.Key))
            .SelectMany(x => x.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matched.Count == 0)
        {
            matched.AddRange(["Communication", "Time management", "Problem solving", "Digital tools", "Portfolio"]);
        }

        return matched
            .Where(skill => !ContainsTerm(userTerms, skill))
            .Take(10)
            .ToList();
    }

    private static IReadOnlySet<string> BuildTermSet(params string?[] values)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var normalizedValue = Normalize(value!);

            foreach (Match match in Regex.Matches(normalizedValue, @"[\p{L}\p{N}#\.+]+"))
            {
                var token = match.Value.Trim();
                if (token.Length >= 2)
                {
                    set.Add(token);
                }
            }

            foreach (var chunk in normalizedValue.Split([',', ';', '|', '/', '\n', '\r'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (chunk.Length >= 2)
                {
                    set.Add(chunk);
                }
            }
        }

        return set;
    }

    private static bool ContainsTerm(IReadOnlySet<string> terms, string value)
    {
        var normalized = Normalize(value);
        return terms.Contains(normalized) || terms.Any(term => normalized.Contains(term, StringComparison.OrdinalIgnoreCase) || term.Contains(normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static int LocationScore(OpportunityTemplate template, ProfessionalProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DesiredLocation))
        {
            return 6;
        }

        var desired = Normalize(request.DesiredLocation);
        if (template.Location.Equals("International", StringComparison.OrdinalIgnoreCase) ||
            desired.Contains("world") ||
            desired.Contains("mundo") ||
            desired.Contains("remote") ||
            desired.Contains("remoto"))
        {
            return 12;
        }

        return Normalize(template.Location).Contains(desired) ||
               desired.Contains(Normalize(template.Location)) ||
               template.Location.Equals("Portugal", StringComparison.OrdinalIgnoreCase)
            ? 14
            : 5;
    }

    private static int EngagementScore(OpportunityTemplate template, ProfessionalProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EngagementType))
        {
            return 4;
        }

        var templateType = Normalize(template.Type);
        var requestedType = Normalize(request.EngagementType);

        if (requestedType.Contains("emprego") && templateType.Contains("full-time"))
        {
            return 12;
        }

        if (requestedType.Contains("prestacao") && templateType.Contains("service"))
        {
            return 12;
        }

        return templateType.Contains(requestedType) || requestedType.Contains(templateType)
            ? 12
            : 4;
    }

    private static string ResolveLocation(string templateLocation, string desiredLocation)
    {
        if (templateLocation.Equals("Preferred city", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(desiredLocation))
        {
            return desiredLocation.Trim();
        }

        return templateLocation;
    }

    private static bool IsFreelanceOrService(string type)
    {
        var normalized = Normalize(type);
        return normalized.Contains("freelance") || normalized.Contains("service") || normalized.Contains("prestacao");
    }

    private static string DetectServiceArea(IReadOnlySet<string> terms)
    {
        if (ContainsTerm(terms, "website") ||
            ContainsTerm(terms, "application") ||
            ContainsTerm(terms, "aplicacao") ||
            ContainsTerm(terms, "developer") ||
            ContainsTerm(terms, "programador") ||
            ContainsTerm(terms, "software"))
        {
            return "Technology";
        }

        if (ContainsTerm(terms, "electrician") ||
            ContainsTerm(terms, "eletricista") ||
            ContainsTerm(terms, "electrical installation") ||
            ContainsTerm(terms, "instalacao eletrica") ||
            ContainsTerm(terms, "electrical panel"))
        {
            return "Electrical work";
        }

        if (ContainsTerm(terms, "logo") ||
            ContainsTerm(terms, "logotipo") ||
            ContainsTerm(terms, "design") ||
            ContainsTerm(terms, "branding") ||
            ContainsTerm(terms, "social media") ||
            ContainsTerm(terms, "redes sociais"))
        {
            return "Design";
        }

        if (ContainsTerm(terms, "accounting") ||
            ContainsTerm(terms, "contabilidade") ||
            ContainsTerm(terms, "tax") ||
            ContainsTerm(terms, "fiscalidade") ||
            ContainsTerm(terms, "vat") ||
            ContainsTerm(terms, "iva"))
        {
            return "Accounting";
        }

        if (ContainsTerm(terms, "autocad") ||
            ContainsTerm(terms, "revit") ||
            ContainsTerm(terms, "3d") ||
            ContainsTerm(terms, "architecture") ||
            ContainsTerm(terms, "arquitetura"))
        {
            return "Technical drafting";
        }

        if (ContainsTerm(terms, "maintenance") ||
            ContainsTerm(terms, "manutencao") ||
            ContainsTerm(terms, "fault") ||
            ContainsTerm(terms, "avaria"))
        {
            return "Maintenance";
        }

        return "Professional service";
    }

    private static string DetectProfessionalType(string serviceArea)
    {
        return serviceArea switch
        {
            "Technology" => "Web developer or software consultant",
            "Electrical work" => "Certified electrician",
            "Design" => "Graphic designer or UI/UX designer",
            "Accounting" => "Certified accountant",
            "Technical drafting" => "Technical drafter / 3D modeler",
            "Maintenance" => "Maintenance technician",
            _ => "Qualified professional"
        };
    }

    private static IReadOnlyList<string> DetectRequiredSkills(string serviceArea, IReadOnlySet<string> terms)
    {
        var skills = serviceArea switch
        {
            "Technology" => new[] { "web", "programming", "responsive design", "seo", "hosting", "maintenance" },
            "Electrical work" => ["electricity", "installations", "safety", "diagnostics", "certification"],
            "Design" => ["figma", "branding", "ui/ux", "social media", "creativity"],
            "Accounting" => ["accounting", "tax", "excel", "erp", "reporting"],
            "Technical drafting" => ["autocad", "revit", "3d modeling", "technical drafting", "bim"],
            "Maintenance" => ["diagnostics", "preventive maintenance", "safety", "technical reporting"],
            _ => ["communication", "quoting", "planning", "execution"]
        };

        return skills
            .Concat(terms.Where(x => x.Length > 3).Take(3))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    private static string DetectComplexity(IReadOnlySet<string> terms, string description)
    {
        if (description.Length > 400 ||
            ContainsTerm(terms, "integration") ||
            ContainsTerm(terms, "integracao") ||
            ContainsTerm(terms, "management") ||
            ContainsTerm(terms, "gestao") ||
            ContainsTerm(terms, "urgent") ||
            ContainsTerm(terms, "urgente"))
        {
            return "Medium/high";
        }

        if (ContainsTerm(terms, "simple") ||
            ContainsTerm(terms, "simples") ||
            ContainsTerm(terms, "small") ||
            ContainsTerm(terms, "pequeno"))
        {
            return "Low";
        }

        return "Medium";
    }

    private static RecommendedProfessional ScoreProfessional(
        ProfessionalTemplate professional,
        IReadOnlyList<string> requiredSkills,
        ServiceRequestRequest request,
        string serviceArea)
    {
        var matchedSkills = requiredSkills
            .Where(skill => professional.Skills.Any(x => ContainsTerm(new HashSet<string>([x], StringComparer.OrdinalIgnoreCase), skill)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var score = 25 + matchedSkills.Count * 10;
        if (request.RemoteAllowed && professional.Location.Equals("Remote", StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
        }
        else if (!string.IsNullOrWhiteSpace(request.Location) && Normalize(professional.Location).Contains(Normalize(request.Location)))
        {
            score += 12;
        }

        if (Normalize(professional.ProfessionalArea).Contains(Normalize(serviceArea)) ||
            professional.Skills.Any(skill => ContainsTerm(new HashSet<string>(requiredSkills, StringComparer.OrdinalIgnoreCase), skill)))
        {
            score += 8;
        }

        return new RecommendedProfessional(
            professional.Name,
            professional.ProfessionalArea,
            professional.Location,
            professional.Availability,
            professional.AveragePrice,
            Math.Clamp(score, 0, 96),
            professional.Skills,
            [
                matchedSkills.Count > 0
                    ? $"Matches {matchedSkills.Count} required skill{(matchedSkills.Count == 1 ? string.Empty : "s")}: {string.Join(", ", matchedSkills.Take(4))}."
                    : "Related professional category for the request.",
                professional.Location.Equals("Remote", StringComparison.OrdinalIgnoreCase)
                    ? "Can work remotely."
                    : $"Available in {professional.Location}.",
                $"Typical price: {professional.AveragePrice}."
            ],
            $"Portfolio includes work in {string.Join(", ", professional.Skills.Take(3))}.");
    }

    private static string BuildClientBrief(
        ServiceRequestRequest request,
        string professionalType,
        IReadOnlyList<string> requiredSkills,
        string complexity,
        string deliveryMode)
    {
        return $"Recommended request for {professionalType}. Complexity: {complexity.ToLowerInvariant()}; delivery: {deliveryMode.ToLowerInvariant()}. Validate experience in {string.Join(", ", requiredSkills.Take(5))}, timeline, budget, work guarantee and previous examples.";
    }

    private static string BuildDisplaySkills(string inputSkills, IReadOnlyList<string> suggestedSkills)
    {
        var skills = inputSkills.Split([',', ';', '\n', '\r'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Concat(suggestedSkills.Take(3))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        return skills.Count == 0 ? "transferable professional skills" : string.Join(", ", skills);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? string.Empty;
    }

    private sealed record OpportunityTemplate(
        string Title,
        string Organization,
        string Type,
        string Location,
        string WorkMode,
        IReadOnlyList<string> RequiredSkills,
        IReadOnlyList<string> NiceToHave,
        string CompensationRange);

    private sealed record ProfessionalTemplate(
        string Name,
        string ProfessionalArea,
        string Location,
        string Availability,
        string AveragePrice,
        IReadOnlyList<string> Skills);
}
