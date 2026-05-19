using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

public class FreelancerServiceListing
{
    public int Id { get; set; }

    public int ProfessionalProfileId { get; set; }

    [Required, StringLength(160)]
    public string ServiceName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [StringLength(2500)]
    public string? Description { get; set; }

    [StringLength(1200)]
    public string? Skills { get; set; }

    public decimal? HourlyRate { get; set; }
    public decimal? ProjectRateFrom { get; set; }
    public decimal? ProjectRateTo { get; set; }

    [Required, StringLength(3)]
    public string Currency { get; set; } = "EUR";

    [StringLength(180)]
    public string? Location { get; set; }

    public bool RemoteAvailable { get; set; } = true;

    [StringLength(120)]
    public string? Availability { get; set; }

    [StringLength(500)]
    public string? PortfolioUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ProfessionalProfile? ProfessionalProfile { get; set; }
    public List<ClientServiceMatch> ClientMatches { get; set; } = new();
}

