using Alivert.Services;

namespace Alivert.Tests;

public class CrmLeadImportServiceTests
{
    [Fact]
    public void Parse_ReadsCsvExportAndDeduplicatesEmails()
    {
        var service = new CrmLeadImportService();

        var rows = service.Parse("""
            name,email,company,role,industry,country,city,stage,tags
            Ana Silva,ana@example.com,Fabrica SA,Operations Manager,Manufacturing,Portugal,Porto,Qualified,industrial
            Ana Silva,ana@example.com,Fabrica SA,Operations Manager,Manufacturing,Portugal,Porto,Qualified,industrial
            invalid,no-email,Fabrica SA,Ops,Manufacturing,Portugal,Porto,Qualified,industrial
            """, "HubSpot");

        var row = Assert.Single(rows);
        Assert.Equal("Ana Silva", row.ContactName);
        Assert.Equal("ana@example.com", row.Email);
        Assert.Equal("Fabrica SA", row.CompanyName);
        Assert.Equal("Qualified", row.Stage);
    }

    [Fact]
    public void Parse_SupportsPortugueseSemicolonHeaders()
    {
        var service = new CrmLeadImportService();

        var rows = service.Parse("""
            nome;email;empresa;cargo;indústria;país;cidade;fase
            João;joao@example.com;Empresa Lda;Diretor;Industria;Portugal;Lisboa;Novo
            """, "Pipedrive");

        var row = Assert.Single(rows);
        Assert.Equal("João", row.ContactName);
        Assert.Equal("Empresa Lda", row.CompanyName);
        Assert.Equal("Portugal", row.Country);
        Assert.Equal("Novo", row.Stage);
    }

    [Fact]
    public void Preview_DetectsColumnsAndSuggestsMappings()
    {
        var service = new CrmLeadImportService();

        var preview = service.Preview("""
            Business Email,Full Name,Organisation,Job Title,Country
            ana@example.com,Ana Silva,Empresa SA,Operations Manager,Portugal
            """);

        Assert.True(preview.HasHeader);
        Assert.Equal(5, preview.Columns.Count);
        Assert.Equal(0, preview.SuggestedMapping.EmailColumn);
        Assert.Equal(1, preview.SuggestedMapping.ContactNameColumn);
        Assert.Equal(2, preview.SuggestedMapping.CompanyNameColumn);
        Assert.Equal(3, preview.SuggestedMapping.RoleColumn);
        Assert.Equal(4, preview.SuggestedMapping.CountryColumn);
    }

    [Fact]
    public void Parse_UsesExplicitColumnMapping()
    {
        var service = new CrmLeadImportService();

        var rows = service.Parse("""
            Empresa;Contacto;Correio;Cidade
            Empresa SA;Ana Silva;ana@example.com;Porto
            """, "CSV", new CrmLeadColumnMapping(
            ExternalIdColumn: null,
            ContactNameColumn: 1,
            EmailColumn: 2,
            PhoneColumn: null,
            CompanyNameColumn: 0,
            RoleColumn: null,
            IndustryColumn: null,
            CountryColumn: null,
            CityColumn: 3,
            StageColumn: null,
            TagsColumn: null,
            SourceColumn: null,
            NotesColumn: null));

        var row = Assert.Single(rows);
        Assert.Equal("Ana Silva", row.ContactName);
        Assert.Equal("ana@example.com", row.Email);
        Assert.Equal("Empresa SA", row.CompanyName);
        Assert.Equal("Porto", row.City);
    }
}
