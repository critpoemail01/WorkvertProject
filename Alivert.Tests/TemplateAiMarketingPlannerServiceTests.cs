using Alivert.Services;

namespace Alivert.Tests;

public class TemplateAiMarketingPlannerServiceTests
{
    [Fact]
    public void Generate_ClipsGeneratedTextToPersistedFieldLimits()
    {
        var service = new TemplateAiMarketingPlannerService();
        var longAudience = "PME industriais, fabricas, empresas de producao, metalomecanicas, manutencao industrial, diretores operacionais, equipas comerciais B2B e gestores que precisam de reduzir tarefas manuais repetitivas";
        var longValue = "automatizar processos comerciais, seguimento de oportunidades, comunicacao com clientes e campanhas recorrentes sem depender de trabalho manual diario";

        var draft = service.Generate(new AiMarketingPlanRequest(
            "SBI Flow",
            "https://sbiflow.example.com",
            "Software para automatizar operacoes e marketing em empresas industriais.",
            longAudience,
            longValue,
            "subscricoes e pedidos de demonstracao",
            "profissional, direto e orientado a resultados",
            ["TikTok", "Instagram", "Facebook", "LinkedIn", "X", "YouTube Shorts"],
            new DateOnly(2026, 5, 13),
            new DateOnly(2026, 6, 12),
            "Daily",
            "ana@example.com\njoao@example.com",
            new AiAudienceLocation("City", "Portugal", "Lisbon", 38.7223, -9.1393, 35)));

        Assert.NotEmpty(draft.Posts);
        Assert.NotEmpty(draft.Emails);
        Assert.NotEmpty(draft.Leads);
        Assert.NotNull(draft.LandingPage);
        Assert.False(string.IsNullOrWhiteSpace(draft.BusinessDna));
        Assert.True(draft.BusinessDna.Length <= 1000);
        Assert.True(draft.LandingPage.Headline.Length <= 180);
        Assert.True(draft.LandingPage.Subheadline.Length <= 260);
        Assert.True(draft.LandingPage.Body.Length <= 1600);
        Assert.True(draft.LandingPage.PrimaryCallToAction.Length <= 120);

        Assert.All(draft.Posts, post =>
        {
            Assert.True(post.Platform.Length <= 40);
            Assert.True(post.Title.Length <= 140);
            Assert.True(post.Hook.Length <= 300);
            Assert.True(post.Caption.Length <= 1600);
            Assert.True(post.CreativeBrief.Length <= 900);
            Assert.True(post.Hashtags is null || post.Hashtags.Length <= 300);
            Assert.True(post.CallToAction.Length <= 180);
        });

        Assert.All(draft.Emails, email =>
        {
            Assert.True(email.Subject.Length <= 160);
            Assert.True(email.PreviewText.Length <= 220);
            Assert.True(email.Body.Length <= 4000);
            Assert.True(email.AudienceSegment.Length <= 160);
        });

        Assert.All(draft.Leads, lead =>
        {
            Assert.True(lead.CompanyProfile.Length <= 160);
            Assert.True(lead.Industry.Length <= 120);
            Assert.True(lead.ContactRole.Length <= 120);
            Assert.True(lead.EmailSearchHint.Length <= 180);
            Assert.True(lead.Reason.Length <= 500);
            Assert.Contains("Lisbon", lead.Reason);
        });
    }
}
