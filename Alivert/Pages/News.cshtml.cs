using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Alivert.Pages;

public sealed class NewsModel : PageModel
{
    public record Post(string Title, string Tag, string Excerpt, DateTime DateUtc);

    public List<Post> Posts { get; } = new()
    {
        new Post(
            "Promovert Plus is live: credit packs for campaign launches",
            "Product",
            "Buy credit packs and keep running campaigns without a subscription.",
            DateTime.UtcNow.AddDays(-7)),
        new Post(
            "Campaign control: cadence and delivery windows",
            "Engineering",
            "We improved scheduling so outreach stays inside the delivery window you choose.",
            DateTime.UtcNow.AddDays(-14)),
        new Post(
            "Roadmap: social publishing and SMS delivery",
            "Roadmap",
            "Next channels are coming. Same campaign workflow, more places to distribute it.",
            DateTime.UtcNow.AddDays(-30)),
    };

    public void OnGet() { }
}
