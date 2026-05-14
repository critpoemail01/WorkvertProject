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
}
