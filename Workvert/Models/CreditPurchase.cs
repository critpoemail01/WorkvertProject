using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

public class CreditPurchase
{
    public int Id { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Range(0, 100000)]
    public int Credits { get; set; }

    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    [Required, StringLength(8)]
    public string Currency { get; set; } = "EUR";

    [Required, StringLength(32)]
    public string Provider { get; set; } = "PayPal";

    [Required, StringLength(24)]
    public string Status { get; set; } = "Pending";

    [Required, StringLength(32)]
    public string PlanCode { get; set; } = "credits";

    [Range(0, 3660)]
    public int SubscriptionDays { get; set; }

    [StringLength(120)]
    public string? ExternalReference { get; set; }

    [StringLength(500)]
    public string? CheckoutUrl { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAtUtc { get; set; }
}
