using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

public class ClientServiceRequest
{
    public int Id { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(120)]
    public string? ServiceArea { get; set; }

    [StringLength(120)]
    public string? ProfessionalTypeNeeded { get; set; }

    [StringLength(40)]
    public string? Complexity { get; set; }

    [StringLength(180)]
    public string? Location { get; set; }

    public bool RemoteAllowed { get; set; } = true;

    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }

    [StringLength(3)]
    public string? Currency { get; set; }

    [StringLength(120)]
    public string? Urgency { get; set; }

    [StringLength(2000)]
    public string? PhotoPaths { get; set; }

    [Required, StringLength(260)]
    public string PhotoUsageNote { get; set; } = "Photos are used only to understand and document the requested service, not to evaluate personal characteristics.";

    [StringLength(1200)]
    public string? RequiredSkills { get; set; }

    [Required, StringLength(32)]
    public string Status { get; set; } = "Open";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<ClientServiceMatch> Matches { get; set; } = new();
}
