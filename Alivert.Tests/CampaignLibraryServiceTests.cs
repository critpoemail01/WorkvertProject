using Alivert.Services;

namespace Alivert.Tests;

public class CampaignLibraryServiceTests
{
    [Fact]
    public void Recommend_ReturnsMixedSectorCampaignsForIndustrialSoftware()
    {
        var service = new CampaignLibraryService();

        var recommendations = service.Recommend(new CampaignLibraryRequest(
            "SBI Flow",
            "Software industrial para operacoes e producao",
            "diretores de operacoes em fabricas",
            "trocar Excel por dashboards e automacao",
            "gerar leads",
            "B2B SaaS / automation"));

        Assert.Contains(recommendations, x => x.Key == "industrial-efficiency");
        Assert.Contains(recommendations, x => x.Key == "b2b-demo");
        Assert.True(recommendations.Select(x => x.Sector).Distinct().Count() > 1);
    }

    [Fact]
    public void Recommend_ReturnsClinicCampaignsForHealthcareBrief()
    {
        var service = new CampaignLibraryService();

        var recommendations = service.Recommend(new CampaignLibraryRequest(
            "Clinica Boa Vida",
            "Clinica dentaria com marcacao online",
            "pacientes locais",
            "consulta rapida",
            "marcacoes",
            "Clinics and healthcare"));

        Assert.Contains(recommendations, x => x.Key == "clinics-first-visit");
        Assert.Contains(recommendations, x => x.Title.Contains("consulta", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Find_ReturnsTemplateByKey()
    {
        var service = new CampaignLibraryService();

        var template = service.Find("construction-quote");

        Assert.NotNull(template);
        Assert.Equal("Construcao e obras", template!.Sector);
        Assert.Contains("orcamento", template.Goal, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Recommend_ReturnsGeneralCampaignsWhenSectorIsUnknown()
    {
        var service = new CampaignLibraryService();

        var recommendations = service.Recommend(new CampaignLibraryRequest(
            "Nova Marca",
            "ideia de negocio ainda sem setor definido",
            "clientes potenciais",
            "proposta clara",
            "gerar leads",
            null));

        Assert.All(recommendations, x => Assert.Equal("Geral / crescimento", x.Sector));
        Assert.Contains(recommendations, x => x.Key == "general-lead-capture");
    }
}
