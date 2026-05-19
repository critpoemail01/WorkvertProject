using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Workvert.Pages;

public sealed class NewsModel : PageModel
{
    public record Post(string Title, string Tag, string Excerpt, DateTime DateUtc);

    public List<Post> Posts { get; } = new()
    {
        new Post(
            "Professional profile assistant",
            "Product",
            "Workvert now analyzes skills, experience, location, and goals to generate career recommendations.",
            DateTime.UtcNow.AddDays(-7)),
        new Post(
            "Flow for clients who need services",
            "Marketplace",
            "Requests written in natural language can now be transformed into an area, professional type, skills, and recommended profiles.",
            DateTime.UtcNow.AddDays(-14)),
        new Post(
            "Roadmap: alerts and real opportunity sources",
            "Roadmap",
            "Upcoming integrations will search for jobs, freelance projects, and service requests on a recurring basis.",
            DateTime.UtcNow.AddDays(-30)),
    };

    public void OnGet() { }
}
