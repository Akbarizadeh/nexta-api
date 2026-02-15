namespace Nexa.Api.Models;

public class Offer
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string? ImageUrl { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public int? DiscountPercent { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int SaveCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Business Business { get; set; } = null!;
    public ICollection<UserInteraction> Interactions { get; set; } = new List<UserInteraction>();
}