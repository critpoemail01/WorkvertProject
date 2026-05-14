namespace Alivert.Services;

public sealed class CampaignLibraryService : ICampaignLibraryService
{
    private static readonly IReadOnlyList<SectorDefinition> Sectors =
    [
        new("clinics", "Clinicas e saude", ["clinic", "clinica", "clínica", "doctor", "dentist", "medical", "health", "saude", "saúde", "consulta", "check-up"],
        [
            Template("clinics-first-visit", "Campanha de primeira consulta", "primeiras consultas marcadas", "primeira consulta com avaliacao inicial", "pessoas na zona que precisam de marcar consulta", ["Instagram", "Facebook", "TikTok", "Email"], 14, "Weekdays", "Educar sobre sintomas, reduzir friccao na marcacao e fechar com prova local.", "Antes/depois, equipa clinica, beneficios da consulta e chamada para marcar.", "Pagina com especialidade, confianca, horarios e CTA para marcar.", "Nome, email, telefone, especialidade pretendida e preferencia de horario.", "Email de confirmacao, lembrete de marcacao e follow-up de disponibilidade.", "consultas pedidas, formularios submetidos e taxa de conversao por canal"),
            Template("clinics-checkup", "Campanha de check-up", "pedidos de check-up", "check-up preventivo com vagas limitadas", "familias, profissionais ocupados e pacientes recorrentes", ["Facebook", "Instagram", "Email"], 21, "ThreePerWeek", "Criar urgencia leve em torno de prevencao e facilidade de marcacao.", "Conteudos educativos curtos e prova de tranquilidade.", "Pagina com pacote de check-up, inclusoes e formulario.", "Nome, contacto, idade aproximada e melhor horario.", "Sequencia de educacao, objecoes e ultima chamada.", "pedidos de check-up, custo por lead e origem dos pedidos"),
            Template("clinics-online-booking", "Campanha de marcacao online", "marcacoes online", "marcacao rapida sem chamada telefonica", "pacientes que preferem reservar online", ["Instagram", "Facebook", "Google", "Email"], 14, "Daily", "Mostrar a simplicidade de marcar e reduzir chamadas perdidas.", "Demonstracao do processo em 3 passos.", "Landing page focada no botao de marcacao.", "Nome, email, telefone e especialidade.", "Confirmacao, lembrete e recuperacao de abandono.", "cliques para marcacao, formularios e reservas confirmadas")
        ]),

        new("construction", "Construcao e obras", ["construction", "construcao", "construção", "obra", "remodel", "renovation", "arquitetura", "architecture", "builder", "orcamento", "orçamento"],
        [
            Template("construction-quote", "Campanha de pedido de orcamento", "pedidos de orcamento qualificados", "orcamento gratuito para obra ou remodelacao", "proprietarios, empresas e gestores de espaco", ["Facebook", "Instagram", "LinkedIn", "Email"], 21, "ThreePerWeek", "Qualificar interessados pela tipologia de obra e localizacao.", "Fotos de obra, processo, checklist e chamada para orcamento.", "Pagina com tipos de obra, prova, zonas servidas e formulario.", "Nome, contacto, local da obra, tipo de obra e prazo.", "Email de triagem, pedido de detalhes e agendamento de visita.", "orcamentos pedidos, visitas agendadas e origem por canal"),
            Template("construction-projects", "Campanha de obras concluidas", "leads com prova social", "portfolio de obras recentes com contacto para proposta", "clientes que precisam de ver confianca antes de pedir proposta", ["Instagram", "Facebook", "LinkedIn"], 30, "Weekdays", "Usar obras reais para construir confianca e gerar pedidos.", "Antes/depois, bastidores e resultados finais.", "Galeria/landing page com casos e CTA para avaliar projeto.", "Nome, email, telefone e tipo de projeto.", "Sequencia com casos parecidos e convite para visita tecnica.", "visualizacoes da landing, leads e casos que mais convertem"),
            Template("construction-remodel", "Campanha de remodelacoes", "pedidos de remodelacao", "avaliacao inicial para remodelacao", "donos de casa, lojas e escritorios", ["Instagram", "Facebook", "TikTok", "Email"], 30, "ThreePerWeek", "Segmentar por problema: cozinha, casa de banho, loja, escritorio.", "Transformacoes visuais e erros comuns a evitar.", "Pagina com tipos de remodelacao e formulario de avaliacao.", "Nome, contacto, local, divisao e budget estimado.", "Follow-up por tipo de remodelacao e prova relevante.", "pedidos por segmento, interacoes e orcamentos iniciados")
        ]),

        new("b2b-software", "Software B2B", ["software", "saas", "b2b", "crm", "erp", "dashboard", "workflow", "automation", "excel", "operations", "industrial", "factory", "fabrica", "fábrica"],
        [
            Template("b2b-demo", "Campanha de demonstracao", "demos marcadas", "demonstracao gratuita orientada ao processo do cliente", "diretores, gestores de operacoes e decisores B2B", ["LinkedIn", "Email", "Instagram"], 14, "Weekdays", "Transformar dor operacional em pedido de demo com prova concreta.", "Problema, workflow, resultado medivel e CTA para demo.", "Pagina com promessa, screenshots, formulario e agenda.", "Nome, email profissional, empresa, cargo e desafio principal.", "Email de contexto, prova, objecoes e convite para demo.", "demos pedidas, empresas atingidas e conversao por canal"),
            Template("b2b-excel", "Campanha troque o Excel por software", "leads que usam processos manuais", "diagnostico gratuito para substituir Excel/processos separados", "equipas que ainda controlam operacoes em folhas ou sistemas dispersos", ["LinkedIn", "Email", "TikTok", "YouTube Shorts"], 21, "ThreePerWeek", "Atacar a dor do Excel e mostrar custo invisivel da operacao manual.", "Comparativos, sinais de caos operacional e mini demo.", "Landing page com checklist de maturidade e formulario.", "Nome, email, empresa, area e processo feito em Excel.", "Sequencia diagnostico, caso de uso, ROI e convite para demo.", "diagnosticos pedidos, interacoes e demos convertidas"),
            Template("b2b-case-study", "Campanha de case study", "leads qualificados por prova", "case study aplicavel ao setor do cliente", "decisores que precisam de prova antes de falar com vendas", ["LinkedIn", "Email", "Facebook"], 30, "Weekly", "Usar prova e narrativa de resultado para leads mais maduros.", "Contexto, problema, solucao, metricas e CTA.", "Landing page com caso, resultado e formulario de acesso.", "Nome, email empresarial, empresa e interesse.", "Entrega do caso, follow-up de insights e convite para conversa.", "downloads/acessos, leads qualificados e pedidos de demo")
        ]),

        new("restaurants", "Restaurantes", ["restaurant", "restaurante", "menu", "reservation", "reserva", "food", "comida", "bar", "cafe", "café", "hospitality"],
        [
            Template("restaurants-weekly-menu", "Campanha menu semanal", "reservas e visitas", "menu semanal em destaque", "clientes locais e visitantes na zona", ["Instagram", "Facebook", "TikTok", "Email"], 7, "Daily", "Gerar visitas com pratos especificos e momentos da semana.", "Prato do dia, bastidores, equipa e reviews.", "Pagina simples com menu, horarios e reserva.", "Nome, contacto, data pretendida e numero de pessoas.", "Lembrete do menu e convite para reservar.", "reservas, cliques no menu e interacoes por prato"),
            Template("restaurants-reservations", "Campanha reservas", "reservas online", "mesa reservada para almoco/jantar", "pessoas que procuram restaurante na zona", ["Instagram", "Facebook", "Google", "Email"], 14, "Weekdays", "Reduzir friccao da reserva e promover horarios com disponibilidade.", "Ambiente, pratos assinatura e chamada para reserva.", "Landing page com reserva e destaques.", "Nome, contacto, data, hora e numero de pessoas.", "Confirmacao, lembrete e upsell de menu.", "reservas, leads e canais com maior conversao"),
            Template("restaurants-private-events", "Campanha eventos privados", "pedidos para eventos", "proposta para aniversarios, empresas ou grupos", "empresas, grupos e organizadores de eventos", ["Instagram", "Facebook", "LinkedIn", "Email"], 30, "ThreePerWeek", "Posicionar o restaurante como solucao para eventos pequenos e medios.", "Espaco, menus de grupo, prova e oferta.", "Pagina com tipos de evento e formulario.", "Nome, contacto, tipo de evento, data e numero de pessoas.", "Email com opcoes de menu e pedido de detalhes.", "pedidos de evento, valor estimado e origem")
        ]),

        new("real-estate", "Imobiliarias", ["real estate", "imobiliaria", "imobiliária", "property", "imovel", "imóvel", "house", "apartamento", "avaliacao", "avaliação", "premium"],
        [
            Template("realestate-valuation", "Campanha avaliacao gratuita", "pedidos de avaliacao", "avaliacao gratuita de imovel", "proprietarios que ponderam vender ou arrendar", ["Facebook", "Instagram", "LinkedIn", "Email"], 21, "ThreePerWeek", "Captar proprietarios antes de colocarem o imovel no mercado.", "Valor de mercado, erros na venda e prova local.", "Pagina com promessa de avaliacao e formulario.", "Nome, contacto, localizacao, tipologia e objetivo.", "Email de confirmacao, checklist e convite para avaliacao.", "avaliacoes pedidas, zonas com procura e leads qualificados"),
            Template("realestate-capture", "Campanha captacao de imoveis", "novos imoveis para carteira", "plano de venda para proprietarios", "donos de imoveis com intencao de vender", ["Facebook", "Instagram", "Email"], 30, "Weekdays", "Mostrar processo de venda e reduzir incerteza do proprietario.", "Prova de vendas, plano de divulgacao e proximos passos.", "Landing page com plano de captacao e formulario.", "Nome, contacto, zona, tipologia e prazo.", "Sequencia de prova, processo e marcacao.", "proprietarios captados, formularios e reunioes marcadas"),
            Template("realestate-premium", "Campanha imoveis premium", "leads compradores premium", "acesso antecipado a imoveis premium", "compradores com poder de compra e investidores", ["Instagram", "LinkedIn", "Facebook", "Email"], 21, "ThreePerWeek", "Criar exclusividade e captar compradores qualificados.", "Lifestyle, detalhes do imovel, zona e escassez.", "Pagina com imovel/colecao e formulario de interesse.", "Nome, contacto, budget, zona e objetivo.", "Email com selecao de imoveis e chamada para visita.", "leads premium, visitas e conversao por imovel")
        ]),

        new("ecommerce", "Ecommerce", ["shop", "store", "ecommerce", "checkout", "cart", "produto", "loja", "buy", "shipping"],
        [
            Template("ecommerce-product-launch", "Campanha lancamento de produto", "compras iniciais", "oferta de lancamento por tempo limitado", "compradores interessados no produto e seguidores da marca", ["Instagram", "TikTok", "Facebook", "Email"], 14, "Daily", "Combinar descoberta visual, prova e urgencia.", "Unboxing, beneficios, prova social e oferta.", "Pagina de produto com formulario/checkout e UTMs.", "Email, interesse, preferencia de produto.", "Sequencia de lancamento, prova e ultima chamada.", "vendas, leads, carrinhos recuperados e ROAS estimado"),
            Template("ecommerce-abandoned-cart", "Campanha recuperacao de carrinho", "carrinhos recuperados", "incentivo para concluir compra", "visitantes que demonstraram interesse", ["Email", "Facebook", "Instagram"], 7, "Daily", "Remover duvidas e recuperar intencao de compra.", "Beneficios, objecoes, reviews e incentivo.", "Pagina com oferta e prova.", "Email e produto de interesse.", "Email 1 lembrete, email 2 prova, email 3 urgencia.", "carrinhos recuperados e receita atribuida")
        ])
    ];

    public IReadOnlyList<SectorCampaignRecommendation> Recommend(CampaignLibraryRequest request, int maxResults = 3)
    {
        var text = string.Join(" ", new[]
        {
            request.ProductName,
            request.CompanyOrIdea,
            request.TargetAudience,
            request.ValueProposition,
            request.CampaignGoal,
            request.DetectedApplicationType
        }).ToLowerInvariant();

        var ranked = Sectors
            .Select(sector => new
            {
                Sector = sector,
                Score = sector.Keywords.Count(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Sector.Name)
            .ToList();

        var selected = ranked.FirstOrDefault(x => x.Score > 0)?.Sector
            ?? Sectors.First(x => x.Key == "b2b-software");

        return selected.Templates.Take(Math.Clamp(maxResults, 1, 8)).ToList();
    }

    public SectorCampaignRecommendation? Find(string key)
    {
        return Sectors
            .SelectMany(x => x.Templates)
            .FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    private static SectorCampaignRecommendation Template(
        string key,
        string title,
        string goal,
        string offer,
        string audience,
        IReadOnlyList<string> platforms,
        int durationDays,
        string frequency,
        string strategy,
        string creativeAngle,
        string landingPageBrief,
        string formBrief,
        string followUpBrief,
        string reportFocus)
    {
        return new SectorCampaignRecommendation(
            key,
            string.Empty,
            title,
            goal,
            offer,
            audience,
            platforms,
            durationDays,
            frequency,
            strategy,
            creativeAngle,
            landingPageBrief,
            formBrief,
            followUpBrief,
            reportFocus);
    }

    private sealed record SectorDefinition(string Key, string Name, IReadOnlyList<string> Keywords, IReadOnlyList<SectorCampaignRecommendation> RawTemplates)
    {
        public IReadOnlyList<SectorCampaignRecommendation> Templates { get; } = RawTemplates
            .Select(template => template with { Sector = Name })
            .ToList();
    }
}
