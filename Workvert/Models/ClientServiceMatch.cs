using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

public class ClientServiceMatch
{
    public int Id { get; set; }

    public int ClientServiceRequestId { get; set; }
    public int FreelancerServiceListingId { get; set; }

    [Range(0, 100)]
    public int CompatibilityScore { get; set; }

    [StringLength(2000)]
    public string? MatchReasons { get; set; }

    [StringLength(1000)]
    public string? SuggestedBrief { get; set; }

    [StringLength(120)]
    public string? EstimatedBudget { get; set; }

    [Required, StringLength(32)]
    public string Status { get; set; } = "Suggested";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ContactedAtUtc { get; set; }
    public DateTime? DismissedAtUtc { get; set; }

    public ClientServiceRequest? ClientServiceRequest { get; set; }
    public FreelancerServiceListing? FreelancerServiceListing { get; set; }
}

