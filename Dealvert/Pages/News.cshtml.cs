using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dealvert.Pages;

public sealed class NewsModel : PageModel
{
    public record Post(string Title, string Tag, string Excerpt, DateTime DateUtc);

    public List<Post> Posts { get; } = new()
    {
        new Post(
            "Credit packs for occasional product watches",
            "Product",
            "Buy credit packs and keep running alerts without a monthly subscription.",
            DateTime.UtcNow.AddDays(-7)),
        new Post(
            "Alert control: cadence and delivery windows",
            "Product",
            "We improved scheduling so price alerts stay inside the delivery window you choose.",
            DateTime.UtcNow.AddDays(-14)),
        new Post(
            "Roadmap: more channels and marketplaces",
            "Roadmap",
            "New sources and delivery channels are on the way. The same alert workflow, with more places to compare and notify.",
            DateTime.UtcNow.AddDays(-30)),
    };

    public void OnGet() { }
}
