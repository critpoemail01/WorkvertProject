using System.Globalization;
using System.Net.Mail;
using System.Text;

namespace Dealvert.Services;

public sealed class CrmLeadImportService
{
    public IReadOnlyList<CrmLeadImportRow> Parse(string? raw, string fallbackSource)
    {
        var document = ReadDocument(raw);
        return document.Lines.Count == 0
            ? []
            : Parse(raw, fallbackSource, document.SuggestedMapping);
    }

    public CsvLeadPreview Preview(string? raw, int sampleSize = 5)
    {
        var document = ReadDocument(raw);
        if (document.Lines.Count == 0)
            return CsvLeadPreview.Empty;

        var sampleRows = document.DataLines
            .Take(sampleSize)
            .Select(line => ParseLine(line, document.Delimiter))
            .ToList();

        var columns = document.Columns
            .Select((column, index) => new CsvColumnPreview(index, column, sampleRows.FirstOrDefault()?.ElementAtOrDefault(index)))
            .ToList();

        return new CsvLeadPreview(
            document.Delimiter,
            document.HasHeader,
            columns,
            sampleRows,
            document.SuggestedMapping);
    }

    public IReadOnlyList<CrmLeadImportRow> Parse(string? raw, string fallbackSource, CrmLeadColumnMapping mapping)
    {
        var document = ReadDocument(raw);
        if (document.Lines.Count == 0 || mapping.EmailColumn is null)
            return [];

        var rows = new List<CrmLeadImportRow>();
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in document.DataLines)
        {
            var columns = ParseLine(line, document.Delimiter);
            var email = Get(columns, mapping.EmailColumn);
            if (!IsEmail(email) || !seenEmails.Add(email!.Trim()))
                continue;

            rows.Add(new CrmLeadImportRow(
                Clip(Get(columns, mapping.ExternalIdColumn), 120),
                Clip(Get(columns, mapping.ContactNameColumn) ?? email, 160) ?? email!,
                email.Trim(),
                Clip(Get(columns, mapping.PhoneColumn), 80),
                Clip(Get(columns, mapping.CompanyNameColumn), 180),
                Clip(Get(columns, mapping.RoleColumn), 120),
                Clip(Get(columns, mapping.IndustryColumn), 120),
                Clip(Get(columns, mapping.CountryColumn), 120),
                Clip(Get(columns, mapping.CityColumn), 160),
                Clip(Get(columns, mapping.StageColumn), 120),
                Clip(Get(columns, mapping.TagsColumn), 300),
                Clip(Get(columns, mapping.SourceColumn) ?? fallbackSource, 120),
                Clip(Get(columns, mapping.NotesColumn), 800)));
        }

        return rows;
    }

    private static readonly Dictionary<string, string> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "externalid",
        ["externalid"] = "externalid",
        ["external_id"] = "externalid",
        ["externalref"] = "externalid",
        ["recordid"] = "externalid",
        ["name"] = "name",
        ["fullname"] = "name",
        ["full_name"] = "name",
        ["person"] = "name",
        ["contact"] = "name",
        ["contactname"] = "name",
        ["contact_name"] = "name",
        ["email"] = "email",
        ["e-mail"] = "email",
        ["mail"] = "email",
        ["emailaddress"] = "email",
        ["email_address"] = "email",
        ["businessmail"] = "email",
        ["businessemail"] = "email",
        ["phone"] = "phone",
        ["mobile"] = "phone",
        ["company"] = "company",
        ["companyname"] = "company",
        ["company_name"] = "company",
        ["organization"] = "company",
        ["organisation"] = "company",
        ["account"] = "company",
        ["role"] = "role",
        ["title"] = "role",
        ["jobtitle"] = "role",
        ["job_title"] = "role",
        ["industry"] = "industry",
        ["sector"] = "industry",
        ["country"] = "country",
        ["city"] = "city",
        ["stage"] = "stage",
        ["status"] = "stage",
        ["tags"] = "tags",
        ["tag"] = "tags",
        ["segment"] = "tags",
        ["segments"] = "tags",
        ["source"] = "source",
        ["notes"] = "notes",
        ["remarks"] = "notes"
    };

    private static CsvDocument ReadDocument(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return CsvDocument.Empty;

        var lines = raw
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count == 0)
            return CsvDocument.Empty;

        var delimiter = DetectDelimiter(lines[0]);
        var first = ParseLine(lines[0], delimiter);
        var laterRows = lines.Skip(1).Take(5).Select(line => ParseLine(line, delimiter)).ToList();
        var hasHeader = HasHeader(first, laterRows);
        var columns = hasHeader
            ? first.Select(value => string.IsNullOrWhiteSpace(value) ? "Unnamed column" : value.Trim()).ToList()
            : first.Select((_, index) => $"Column {index + 1}").ToList();
        var dataLines = hasHeader ? lines.Skip(1).ToList() : lines;

        return new CsvDocument(
            delimiter,
            hasHeader,
            lines,
            dataLines,
            columns,
            BuildSuggestedMapping(columns, dataLines.Take(8).Select(line => ParseLine(line, delimiter)).ToList()));
    }

    private static bool HasHeader(IReadOnlyList<string> first, IReadOnlyList<IReadOnlyList<string>> laterRows)
    {
        if (first.Any(value => HeaderAliases.ContainsKey(NormalizeHeader(value))))
            return true;

        return !first.Any(IsEmail) && laterRows.Any(row => row.Any(IsEmail));
    }

    private static CrmLeadColumnMapping BuildSuggestedMapping(
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyList<string>> samples)
    {
        return new CrmLeadColumnMapping(
            FindColumn(columns, samples, "externalid"),
            FindColumn(columns, samples, "name"),
            FindColumn(columns, samples, "email"),
            FindColumn(columns, samples, "phone"),
            FindColumn(columns, samples, "company"),
            FindColumn(columns, samples, "role"),
            FindColumn(columns, samples, "industry"),
            FindColumn(columns, samples, "country"),
            FindColumn(columns, samples, "city"),
            FindColumn(columns, samples, "stage"),
            FindColumn(columns, samples, "tags"),
            FindColumn(columns, samples, "source"),
            FindColumn(columns, samples, "notes"));
    }

    private static int? FindColumn(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> samples, string key)
    {
        for (var index = 0; index < columns.Count; index++)
        {
            var normalized = NormalizeHeader(columns[index]);
            var alias = HeaderAliases.TryGetValue(normalized, out var mapped) ? mapped : normalized;
            if (alias.Equals(key, StringComparison.OrdinalIgnoreCase))
                return index;
        }

        if (key == "email")
        {
            for (var index = 0; index < columns.Count; index++)
            {
                if (samples.Any(row => index < row.Count && IsEmail(row[index])))
                    return index;
            }
        }

        return null;
    }

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

    private static string? Get(IReadOnlyList<string> values, int? column)
    {
        return column is >= 0 && column.Value < values.Count && !string.IsNullOrWhiteSpace(values[column.Value])
            ? values[column.Value].Trim()
            : null;
    }

    private static string NormalizeHeader(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark ||
                ch is ' ' or '-' or '_' or '\u00ad')
            {
                continue;
            }

            if (ch <= 127)
                builder.Append(ch);
        }

        return builder.ToString();
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

internal sealed record CsvDocument(
    char Delimiter,
    bool HasHeader,
    IReadOnlyList<string> Lines,
    IReadOnlyList<string> DataLines,
    IReadOnlyList<string> Columns,
    CrmLeadColumnMapping SuggestedMapping)
{
    public static CsvDocument Empty { get; } = new(',', true, [], [], [], CrmLeadColumnMapping.Empty);
}

public sealed record CsvLeadPreview(
    char Delimiter,
    bool HasHeader,
    IReadOnlyList<CsvColumnPreview> Columns,
    IReadOnlyList<IReadOnlyList<string>> SampleRows,
    CrmLeadColumnMapping SuggestedMapping)
{
    public static CsvLeadPreview Empty { get; } = new(',', true, [], [], CrmLeadColumnMapping.Empty);
}

public sealed record CsvColumnPreview(int Index, string Label, string? SampleValue);

public sealed record CrmLeadColumnMapping(
    int? ExternalIdColumn,
    int? ContactNameColumn,
    int? EmailColumn,
    int? PhoneColumn,
    int? CompanyNameColumn,
    int? RoleColumn,
    int? IndustryColumn,
    int? CountryColumn,
    int? CityColumn,
    int? StageColumn,
    int? TagsColumn,
    int? SourceColumn,
    int? NotesColumn)
{
    public static CrmLeadColumnMapping Empty { get; } = new(null, null, null, null, null, null, null, null, null, null, null, null, null);
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
