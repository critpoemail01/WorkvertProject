using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Alivert.Pages;

public sealed class NewsModel : PageModel
{
    public record Post(string Title, string Tag, string Excerpt, DateTime DateUtc);

    public List<Post> Posts { get; } = new()
    {
        new Post(
            "Pacotes de creditos para lancamentos de campanha",
            "Produto",
            "Compra pacotes de creditos e continua a correr campanhas sem subscricao mensal.",
            DateTime.UtcNow.AddDays(-7)),
        new Post(
            "Controlo de campanha: cadencia e janelas de entrega",
            "Produto",
            "Melhoramos o agendamento para manter outreach dentro da janela de entrega escolhida.",
            DateTime.UtcNow.AddDays(-14)),
        new Post(
            "Roadmap: publicacao social e envio SMS",
            "Roadmap",
            "Novos canais estao a caminho. O mesmo workflow de campanha, com mais sitios para distribuir.",
            DateTime.UtcNow.AddDays(-30)),
    };

    public void OnGet() { }
}
