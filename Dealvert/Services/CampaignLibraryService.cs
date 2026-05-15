using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Alivert.Services;

public sealed class CampaignLibraryService : ICampaignLibraryService
{
    private const string GeneralSectorKey = "general-growth";

    private static readonly IReadOnlyList<SectorDefinition> Sectors =
    [
        new(GeneralSectorKey, "Geral / crescimento", [],
        [
            Template("general-lead-capture", "Campanha de captacao de leads", "leads qualificados", "diagnostico, proposta ou contacto inicial", "potenciais clientes com interesse no produto ou servico", ["LinkedIn", "Instagram", "Facebook", "Email"], 14, "ThreePerWeek", "Explicar a dor, apresentar a promessa e converter visitantes numa conversa comercial.", "Conteudo educativo, prova curta, objecoes e CTA direto.", "Landing page com promessa, beneficios, prova e formulario simples.", "Nome, email, empresa, necessidade e melhor contacto.", "Email de contexto, prova, objecoes e convite para falar.", "leads, taxa de conversao, canal que mais gerou contactos e proxima acao"),
            Template("general-launch", "Campanha de lancamento", "primeiros clientes ou subscritores", "oferta de lancamento por tempo limitado", "publico-alvo inicial com maior probabilidade de experimentar a solucao", ["Instagram", "TikTok", "LinkedIn", "Email"], 21, "Daily", "Criar reconhecimento, explicar a oferta e dar uma razao clara para agir agora.", "Problema, novidade, beneficio, prova e urgencia leve.", "Pagina de lancamento com oferta, CTA e tracking por canal.", "Nome, email, segmento e interesse principal.", "Sequencia de lancamento, prova social e ultima chamada.", "alcance, cliques, leads, subscricoes/compras e melhor mensagem"),
            Template("general-proof", "Campanha de prova social", "leads mais confiantes", "caso, testemunho ou resultado demonstravel", "clientes que precisam de confianca antes de comprar ou subscrever", ["LinkedIn", "Facebook", "Instagram", "Email"], 30, "Weekly", "Usar resultados, exemplos e testemunhos para reduzir risco percebido.", "Antes/depois, historia de cliente, numeros e CTA para o proximo passo.", "Landing page com caso, resultado, perguntas frequentes e formulario.", "Nome, contacto, empresa/necessidade e contexto.", "Entrega do caso, follow-up de insights e convite para proposta.", "leads influenciados por prova social, conversao da landing e melhor canal")
        ]),

        new("clinics", "Clinicas e saude", ["clinic", "clinica", "doctor", "dentist", "medical", "health", "saude", "consulta", "check-up"],
        [
            Template("clinics-first-visit", "Campanha de primeira consulta", "primeiras consultas marcadas", "primeira consulta com avaliacao inicial", "pessoas na zona que precisam de marcar consulta", ["Instagram", "Facebook", "TikTok", "Email"], 14, "Weekdays", "Educar sobre sintomas, reduzir friccao na marcacao e fechar com prova local.", "Antes/depois, equipa clinica, beneficios da consulta e chamada para marcar.", "Pagina com especialidade, confianca, horarios e CTA para marcar.", "Nome, email, telefone, especialidade pretendida e preferencia de horario.", "Email de confirmacao, lembrete de marcacao e follow-up de disponibilidade.", "consultas pedidas, formularios submetidos e taxa de conversao por canal"),
            Template("clinics-checkup", "Campanha de check-up", "pedidos de check-up", "check-up preventivo com vagas limitadas", "familias, profissionais ocupados e pacientes recorrentes", ["Facebook", "Instagram", "Email"], 21, "ThreePerWeek", "Criar urgencia leve em torno de prevencao e facilidade de marcacao.", "Conteudos educativos curtos e prova de tranquilidade.", "Pagina com pacote de check-up, inclusoes e formulario.", "Nome, contacto, idade aproximada e melhor horario.", "Sequencia de educacao, objecoes e ultima chamada.", "pedidos de check-up, custo por lead e origem dos pedidos"),
            Template("clinics-online-booking", "Campanha de marcacao online", "marcacoes online", "marcacao rapida sem chamada telefonica", "pacientes que preferem reservar online", ["Instagram", "Facebook", "Google", "Email"], 14, "Daily", "Mostrar a simplicidade de marcar e reduzir chamadas perdidas.", "Demonstracao do processo em 3 passos.", "Landing page focada no botao de marcacao.", "Nome, email, telefone e especialidade.", "Confirmacao, lembrete e recuperacao de abandono.", "cliques para marcacao, formularios e reservas confirmadas")
        ]),

        new("construction", "Construcao e obras", ["construction", "construcao", "obra", "remodel", "renovation", "arquitetura", "architecture", "builder", "orcamento"],
        [
            Template("construction-quote", "Campanha de pedido de orcamento", "pedidos de orcamento qualificados", "orcamento gratuito para obra ou remodelacao", "proprietarios, empresas e gestores de espaco", ["Facebook", "Instagram", "LinkedIn", "Email"], 21, "ThreePerWeek", "Qualificar interessados pela tipologia de obra e localizacao.", "Fotos de obra, processo, checklist e chamada para orcamento.", "Pagina com tipos de obra, prova, zonas servidas e formulario.", "Nome, contacto, local da obra, tipo de obra e prazo.", "Email de triagem, pedido de detalhes e agendamento de visita.", "orcamentos pedidos, visitas agendadas e origem por canal"),
            Template("construction-projects", "Campanha de obras concluidas", "leads com prova social", "portfolio de obras recentes com contacto para proposta", "clientes que precisam de ver confianca antes de pedir proposta", ["Instagram", "Facebook", "LinkedIn"], 30, "Weekdays", "Usar obras reais para construir confianca e gerar pedidos.", "Antes/depois, bastidores e resultados finais.", "Galeria/landing page com casos e CTA para avaliar projeto.", "Nome, email, telefone e tipo de projeto.", "Sequencia com casos parecidos e convite para visita tecnica.", "visualizacoes da landing, leads e casos que mais convertem"),
            Template("construction-remodel", "Campanha de remodelacoes", "pedidos de remodelacao", "avaliacao inicial para remodelacao", "donos de casa, lojas e escritorios", ["Instagram", "Facebook", "TikTok", "Email"], 30, "ThreePerWeek", "Segmentar por problema: cozinha, casa de banho, loja, escritorio.", "Transformacoes visuais e erros comuns a evitar.", "Pagina com tipos de remodelacao e formulario de avaliacao.", "Nome, contacto, local, divisao e budget estimado.", "Follow-up por tipo de remodelacao e prova relevante.", "pedidos por segmento, interacoes e orcamentos iniciados")
        ]),

        new("industrial-operations", "Industria e operacoes", ["industrial", "industry", "manufacturing", "factory", "fabrica", "producao", "operacoes", "maintenance", "manutencao", "metalomecanica", "energia", "planeamento", "qualidade"],
        [
            Template("industrial-efficiency", "Campanha de eficiencia operacional", "leads de melhoria operacional", "avaliacao gratuita de eficiencia ou diagnostico de processo", "diretores de operacoes, producao, manutencao e gestores industriais", ["LinkedIn", "Email", "Facebook"], 21, "ThreePerWeek", "Ligar problemas de producao, energia, manutencao ou planeamento a ganhos mensuraveis.", "Dor operacional, custo invisivel, checklist e exemplo de melhoria.", "Landing page com diagnostico, setores atendidos e formulario qualificado.", "Nome, email profissional, empresa, funcao, area e principal gargalo.", "Email com checklist, caso semelhante e convite para diagnostico.", "diagnosticos pedidos, leads por setor e oportunidades qualificadas"),
            Template("industrial-maintenance", "Campanha manutencao e paragens", "pedidos de contacto sobre manutencao", "plano para reduzir paragens e atrasos", "responsaveis de manutencao, producao e planeamento industrial", ["LinkedIn", "Email", "Instagram"], 14, "Weekdays", "Atacar o impacto de paragens, retrabalho e informacao dispersa.", "Sinais de risco, rotinas preventivas, mini caso e CTA para conversa.", "Pagina focada em manutencao, disponibilidade e impacto financeiro.", "Nome, empresa, cargo, tipo de operacao e problema atual.", "Sequencia educacional, prova e convite para avaliar o processo.", "leads de manutencao, cliques de decisores e taxa de conversao"),
            Template("industrial-planning", "Campanha planeamento de producao", "reunioes sobre planeamento", "sessao de melhoria de planeamento e custos", "PME industriais, fabricas e equipas com processos de planeamento manuais", ["LinkedIn", "Email", "YouTube Shorts"], 30, "ThreePerWeek", "Mostrar como a falta de visibilidade afeta prazos, energia, custos e decisoes.", "Comparativos, erros comuns, antes/depois e CTA para sessao.", "Landing page com desafios de planeamento e formulario de qualificacao.", "Nome, email, empresa, dimensao da operacao e ferramenta atual.", "Email de diagnostico, ROI, caso de uso e marcacao.", "reunioes marcadas, leads qualificados e canal com maior intencao")
        ]),

        new("b2b-software", "Software B2B", ["software", "saas", "b2b", "crm", "erp", "dashboard", "workflow", "automation", "automacao", "excel", "sistema", "plataforma"],
        [
            Template("b2b-demo", "Campanha de demonstracao", "demos marcadas", "demonstracao gratuita orientada ao processo do cliente", "diretores, gestores de operacoes e decisores B2B", ["LinkedIn", "Email", "Instagram"], 14, "Weekdays", "Transformar dor operacional em pedido de demo com prova concreta.", "Problema, workflow, resultado medivel e CTA para demo.", "Pagina com promessa, screenshots, formulario e agenda.", "Nome, email profissional, empresa, cargo e desafio principal.", "Email de contexto, prova, objecoes e convite para demo.", "demos pedidas, empresas atingidas e conversao por canal"),
            Template("b2b-excel", "Campanha troque o Excel por software", "leads que usam processos manuais", "diagnostico gratuito para substituir Excel/processos separados", "equipas que ainda controlam operacoes em folhas ou sistemas dispersos", ["LinkedIn", "Email", "TikTok", "YouTube Shorts"], 21, "ThreePerWeek", "Atacar a dor do Excel e mostrar custo invisivel da operacao manual.", "Comparativos, sinais de caos operacional e mini demo.", "Landing page com checklist de maturidade e formulario.", "Nome, email, empresa, area e processo feito em Excel.", "Sequencia diagnostico, caso de uso, ROI e convite para demo.", "diagnosticos pedidos, interacoes e demos convertidas"),
            Template("b2b-case-study", "Campanha de case study", "leads qualificados por prova", "case study aplicavel ao setor do cliente", "decisores que precisam de prova antes de falar com vendas", ["LinkedIn", "Email", "Facebook"], 30, "Weekly", "Usar prova e narrativa de resultado para leads mais maduros.", "Contexto, problema, solucao, metricas e CTA.", "Landing page com caso, resultado e formulario de acesso.", "Nome, email empresarial, empresa e interesse.", "Entrega do caso, follow-up de insights e convite para conversa.", "downloads/acessos, leads qualificados e pedidos de demo")
        ]),

        new("restaurants", "Restaurantes", ["restaurant", "restaurante", "menu", "reservation", "reserva", "food", "comida", "bar", "cafe", "hospitality"],
        [
            Template("restaurants-weekly-menu", "Campanha menu semanal", "reservas e visitas", "menu semanal em destaque", "clientes locais e visitantes na zona", ["Instagram", "Facebook", "TikTok", "Email"], 7, "Daily", "Gerar visitas com pratos especificos e momentos da semana.", "Prato do dia, bastidores, equipa e reviews.", "Pagina simples com menu, horarios e reserva.", "Nome, contacto, data pretendida e numero de pessoas.", "Lembrete do menu e convite para reservar.", "reservas, cliques no menu e interacoes por prato"),
            Template("restaurants-reservations", "Campanha reservas", "reservas online", "mesa reservada para almoco/jantar", "pessoas que procuram restaurante na zona", ["Instagram", "Facebook", "Google", "Email"], 14, "Weekdays", "Reduzir friccao da reserva e promover horarios com disponibilidade.", "Ambiente, pratos assinatura e chamada para reserva.", "Landing page com reserva e destaques.", "Nome, contacto, data, hora e numero de pessoas.", "Confirmacao, lembrete e upsell de menu.", "reservas, leads e canais com maior conversao"),
            Template("restaurants-private-events", "Campanha eventos privados", "pedidos para eventos", "proposta para aniversarios, empresas ou grupos", "empresas, grupos e organizadores de eventos", ["Instagram", "Facebook", "LinkedIn", "Email"], 30, "ThreePerWeek", "Posicionar o restaurante como solucao para eventos pequenos e medios.", "Espaco, menus de grupo, prova e oferta.", "Pagina com tipos de evento e formulario.", "Nome, contacto, tipo de evento, data e numero de pessoas.", "Email com opcoes de menu e pedido de detalhes.", "pedidos de evento, valor estimado e origem")
        ]),

        new("real-estate", "Imobiliarias", ["real estate", "imobiliaria", "property", "imovel", "house", "apartamento", "avaliacao", "premium"],
        [
            Template("realestate-valuation", "Campanha avaliacao gratuita", "pedidos de avaliacao", "avaliacao gratuita de imovel", "proprietarios que ponderam vender ou arrendar", ["Facebook", "Instagram", "LinkedIn", "Email"], 21, "ThreePerWeek", "Captar proprietarios antes de colocarem o imovel no mercado.", "Valor de mercado, erros na venda e prova local.", "Pagina com promessa de avaliacao e formulario.", "Nome, contacto, localizacao, tipologia e objetivo.", "Email de confirmacao, checklist e convite para avaliacao.", "avaliacoes pedidas, zonas com procura e leads qualificados"),
            Template("realestate-capture", "Campanha captacao de imoveis", "novos imoveis para carteira", "plano de venda para proprietarios", "donos de imoveis com intencao de vender", ["Facebook", "Instagram", "Email"], 30, "Weekdays", "Mostrar processo de venda e reduzir incerteza do proprietario.", "Prova de vendas, plano de divulgacao e proximos passos.", "Landing page com plano de captacao e formulario.", "Nome, contacto, zona, tipologia e prazo.", "Sequencia de prova, processo e marcacao.", "proprietarios captados, formularios e reunioes marcadas"),
            Template("realestate-premium", "Campanha imoveis premium", "leads compradores premium", "acesso antecipado a imoveis premium", "compradores com poder de compra e investidores", ["Instagram", "LinkedIn", "Facebook", "Email"], 21, "ThreePerWeek", "Criar exclusividade e captar compradores qualificados.", "Lifestyle, detalhes do imovel, zona e escassez.", "Pagina com imovel/colecao e formulario de interesse.", "Nome, contacto, budget, zona e objetivo.", "Email com selecao de imoveis e chamada para visita.", "leads premium, visitas e conversao por imovel")
        ]),

        new("ecommerce", "Ecommerce", ["shop", "store", "ecommerce", "checkout", "cart", "produto", "loja", "buy", "shipping"],
        [
            Template("ecommerce-product-launch", "Campanha lancamento de produto", "compras iniciais", "oferta de lancamento por tempo limitado", "compradores interessados no produto e seguidores da marca", ["Instagram", "TikTok", "Facebook", "Email"], 14, "Daily", "Combinar descoberta visual, prova e urgencia.", "Unboxing, beneficios, prova social e oferta.", "Pagina de produto com formulario/checkout e UTMs.", "Email, interesse, preferencia de produto.", "Sequencia de lancamento, prova e ultima chamada.", "vendas, leads, carrinhos recuperados e ROAS estimado"),
            Template("ecommerce-abandoned-cart", "Campanha recuperacao de carrinho", "carrinhos recuperados", "incentivo para concluir compra", "visitantes que demonstraram interesse", ["Email", "Facebook", "Instagram"], 7, "Daily", "Remover duvidas e recuperar intencao de compra.", "Beneficios, objecoes, reviews e incentivo.", "Pagina com oferta e prova.", "Email e produto de interesse.", "Email 1 lembrete, email 2 prova, email 3 urgencia.", "carrinhos recuperados e receita atribuida")
        ]),

        new("education", "Educacao e cursos", ["course", "curso", "learn", "training", "formacao", "school", "academy", "aula", "student", "educacao"],
        [
            Template("education-enrolment", "Campanha inscricoes", "inscricoes em curso ou formacao", "aula aberta ou sessao gratuita", "profissionais, estudantes ou equipas que querem aprender uma competencia", ["LinkedIn", "Instagram", "YouTube Shorts", "Email"], 21, "ThreePerWeek", "Mostrar resultado pratico da aprendizagem e reduzir duvidas antes da inscricao.", "Aula curta, progresso, exemplos de alunos e CTA para inscricao.", "Pagina com programa, resultado, professor e formulario.", "Nome, email, objetivo de aprendizagem e nivel atual.", "Email com conteudo util, prova e convite para aula aberta.", "inscricoes, leads, taxa de conversao e modulo com maior interesse"),
            Template("education-webinar", "Campanha webinar", "registos para webinar", "webinar gratuito com tema especifico", "publico que precisa de resolver uma duvida concreta antes de comprar", ["LinkedIn", "Email", "Instagram"], 14, "Weekdays", "Usar um evento educativo para captar leads e preparar venda posterior.", "Problema, promessa do webinar, agenda e prova do especialista.", "Landing page de registo com calendario e lembretes.", "Nome, email, empresa/opcional e pergunta principal.", "Confirmacao, lembretes, replay e oferta final.", "registos, presencas, perguntas e vendas/subscricoes depois do webinar")
        ]),

        new("professional-services", "Servicos profissionais", ["consultoria", "consulting", "agency", "agencia", "law", "legal", "accounting", "contabilidade", "servico", "services", "auditoria"],
        [
            Template("services-assessment", "Campanha avaliacao inicial", "pedidos de avaliacao", "avaliacao inicial gratuita ou triagem", "empresas ou clientes que precisam de escolher um prestador de confianca", ["LinkedIn", "Facebook", "Instagram", "Email"], 14, "ThreePerWeek", "Criar confianca com diagnostico, processo e prova de experiencia.", "Erros comuns, checklist, prova e CTA para avaliacao.", "Landing page com servicos, metodo, casos e formulario.", "Nome, email, empresa, necessidade e urgencia.", "Email de triagem, prova, objecoes e convite para reuniao.", "pedidos de avaliacao, taxa de reuniao e melhor canal"),
            Template("services-retainer", "Campanha contrato recorrente", "leads para servico recorrente", "plano mensal ou acompanhamento continuo", "clientes que precisam de apoio regular e previsivel", ["LinkedIn", "Email", "Facebook"], 30, "Weekly", "Mostrar o custo de resolver tudo pontualmente versus acompanhamento continuo.", "Comparativo, processo, casos e beneficios da recorrencia.", "Pagina com pacotes, entregaveis e formulario de qualificacao.", "Nome, contacto, negocio, necessidade e dimensao.", "Sequencia educativa, proposta de valor e chamada de diagnostico.", "leads recorrentes, reunioes e valor potencial de contrato")
        ])
    ];

    public IReadOnlyList<SectorCampaignRecommendation> Recommend(CampaignLibraryRequest request, int maxResults = 3)
    {
        var text = NormalizeForSearch(string.Join(" ", new[]
        {
            request.ProductName,
            request.CompanyOrIdea,
            request.TargetAudience,
            request.ValueProposition,
            request.CampaignGoal,
            request.DetectedApplicationType
        }));

        var ranked = Sectors
            .Where(sector => !sector.Key.Equals(GeneralSectorKey, StringComparison.OrdinalIgnoreCase))
            .Select(sector => new
            {
                Sector = sector,
                Score = sector.Keywords.Count(keyword => KeywordMatches(text, keyword))
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Sector.Name)
            .ToList();

        var selectedSectors = ranked
            .Where(x => x.Score > 0)
            .Select(x => x.Sector)
            .ToList();

        var general = Sectors.First(x => x.Key == GeneralSectorKey);
        if (selectedSectors.Count == 0)
            selectedSectors.Add(general);
        else
            selectedSectors.Add(general);

        return PickTemplates(selectedSectors, Math.Clamp(maxResults, 1, 8));
    }

    public SectorCampaignRecommendation? Find(string key)
    {
        return Sectors
            .SelectMany(x => x.Templates)
            .FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<SectorCampaignRecommendation> PickTemplates(
        IReadOnlyList<SectorDefinition> sectors,
        int maxResults)
    {
        var recommendations = new List<SectorCampaignRecommendation>();
        var templateIndex = 0;

        while (recommendations.Count < maxResults)
        {
            var added = false;
            foreach (var sector in sectors)
            {
                if (templateIndex >= sector.Templates.Count)
                    continue;

                var template = sector.Templates[templateIndex];
                if (recommendations.Any(x => x.Key.Equals(template.Key, StringComparison.OrdinalIgnoreCase)))
                    continue;

                recommendations.Add(template);
                added = true;
                if (recommendations.Count == maxResults)
                    break;
            }

            if (!added)
                break;

            templateIndex++;
        }

        return recommendations;
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

    private static bool KeywordMatches(string text, string keyword)
    {
        var normalizedKeyword = NormalizeForSearch(keyword);
        return normalizedKeyword.Length <= 3
            ? Regex.IsMatch(text, $@"\b{Regex.Escape(normalizedKeyword)}\b", RegexOptions.IgnoreCase)
            : text.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeForSearch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private sealed record SectorDefinition(string Key, string Name, IReadOnlyList<string> Keywords, IReadOnlyList<SectorCampaignRecommendation> RawTemplates)
    {
        public IReadOnlyList<SectorCampaignRecommendation> Templates { get; } = RawTemplates
            .Select(template => template with { Sector = Name })
            .ToList();
    }
}
