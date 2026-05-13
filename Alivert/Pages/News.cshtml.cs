using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Alivert.Pages;

public sealed class NewsModel : PageModel
{
    public record Post(string Title, string Tag, string Excerpt, DateTime DateUtc);

    public List<Post> Posts { get; } = new()
    {
        new Post(
            "Alivert Plus is live: credit packs for casual users",
            "Product",
            "Buy credit packs and keep using alerts without a subscription.",
            DateTime.UtcNow.AddDays(-7)),
        new Post(
            "Noise control: cooldown + dedup to avoid spam",
            "Engineering",
            "We improved the evaluator so repeated triggers do not flood your inbox.",
            DateTime.UtcNow.AddDays(-14)),
        new Post(
            "Roadmap: Telegram + Discord notifications",
            "Roadmap",
            "Next channels are coming. Same alerts, more places to receive them.",
            DateTime.UtcNow.AddDays(-30)),
    };

    public void OnGet() { }
}
