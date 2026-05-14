using System.Net.Mail;

namespace Alivert.Services;

public sealed class CrmLeadImportService
{
    public IReadOnlyList<CrmLeadImportRow> Parse(string? raw, string fallbackSource)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        var lines = raw
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count == 0)
            return [];

        var delimiter = DetectDelimiter(lines[0]);
        var first = ParseLine(lines[0], delimiter);
        var hasHeader = first.Any(value => HeaderAliases.ContainsKey(NormalizeHeader(value)));
        var headers = hasHeader
            ? first.Select(NormalizeHeader).ToList()
            : new List<string> { "email", "name", "company", "role", "industry", "country", "city", "stage", "tags", "notes" };
        var dataLines = hasHeader ? lines.Skip(1) : lines;
        var rows = new List<CrmLeadImportRow>();
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in dataLines)
        {
            var columns = ParseLine(line, delimiter);
            var email = Get(columns, headers, "email");
            if (!IsEmail(email) || !seenEmails.Add(email!.Trim()))
                continue;

            rows.Add(new CrmLeadImportRow(
                Clip(Get(columns, headers, "externalid"), 120),
                Clip(Get(columns, headers, "name") ?? email, 160) ?? email!,
                email.Trim(),
                Clip(Get(columns, headers, "phone"), 80),
                Clip(Get(columns, headers, "company"), 180),
                Clip(Get(columns, headers, "role"), 120),
                Clip(Get(columns, headers, "industry"), 120),
                Clip(Get(columns, headers, "country"), 120),
                Clip(Get(columns, headers, "city"), 160),
                Clip(Get(columns, headers, "stage"), 120),
                Clip(Get(columns, headers, "tags"), 300),
                Clip(Get(columns, headers, "source") ?? fallbackSource, 120),
                Clip(Get(columns, headers, "notes"), 800)));
        }

        return rows;
    }

    private static readonly Dictionary<string, string> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "externalid",
        ["externalid"] = "externalid",
        ["external_id"] = "externalid",
        ["nome"] = "name",
        ["name"] = "name",
        ["contact"] = "name",
        ["contactname"] = "name",
        ["contact_name"] = "name",
        ["email"] = "email",
        ["e-mail"] = "email",
        ["mail"] = "email",
        ["phone"] = "phone",
        ["telefone"] = "phone",
        ["telemovel"] = "phone",
        ["telemóvel"] = "phone",
        ["company"] = "company",
        ["companyname"] = "company",
        ["company_name"] = "company",
        ["empresa"] = "company",
        ["role"] = "role",
        ["title"] = "role",
        ["cargo"] = "role",
        ["industry"] = "industry",
        ["industria"] = "industry",
        ["indústria"] = "industry",
        ["sector"] = "industry",
        ["country"] = "country",
        ["pais"] = "country",
        ["país"] = "country",
        ["city"] = "city",
        ["cidade"] = "city",
        ["stage"] = "stage",
        ["fase"] = "stage",
        ["status"] = "stage",
        ["estado"] = "stage",
        ["tags"] = "tags",
        ["tag"] = "tags",
        ["source"] = "source",
        ["fonte"] = "source",
        ["notes"] = "notes",
        ["notas"] = "notes",
        ["observacoes"] = "notes",
        ["observações"] = "notes"
    };

    private static char DetectDelimiter(string line)
    {
        var semicolons = line.Count(ch => ch == ';');
        var commas = line.Count(ch => ch == ',');
        return semicolons > commas ? ';' : ',';
    }

    private static List<string> ParseLine(string line, char delimiter)
    {
        var values = new List<string>();
        var current = new List<char>();
        var quoted = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Add('"');
                    i++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (ch == delimiter && !quoted)
            {
                values.Add(new string(current.ToArray()).Trim());
                current.Clear();
            }
            else
            {
                current.Add(ch);
            }
        }

        values.Add(new string(current.ToArray()).Trim());
        return values;
    }

    private static string? Get(IReadOnlyList<string> values, IReadOnlyList<string> headers, string key)
    {
        for (var i = 0; i < headers.Count && i < values.Count; i++)
        {
            var normalized = HeaderAliases.TryGetValue(headers[i], out var alias) ? alias : headers[i];
            if (normalized.Equals(key, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(values[i]))
                return values[i].Trim();
        }

        return null;
    }

    private static string NormalizeHeader(string value)
    {
        return value.Trim().ToLowerInvariant().Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private static bool IsEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            _ = new MailAddress(value.Trim());
            return value.Contains('@', StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? Clip(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var text = value.Trim();
        return text.Length <= maxLength ? text : text[..maxLength].TrimEnd();
    }
}

public sealed record CrmLeadImportRow(
    string? ExternalId,
    string ContactName,
    string Email,
    string? Phone,
    string? CompanyName,
    string? Role,
    string? Industry,
    string? Country,
    string? City,
    string? Stage,
    string? Tags,
    string? Source,
    string? Notes);
