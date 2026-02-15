namespace Nexa.Api.Models;

public class Listing
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public Guid? BusinessId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public decimal? Price { get; set; }
    public ListingType Type { get; set; } = ListingType.Product;
    public ListingStatus Status { get; set; } = ListingStatus.Active;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? AiConfidenceScore { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int SaveCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Seller { get; set; } = null!;
    public Business? Business { get; set; }
    public ICollection<UserInteraction> Interactions { get; set; } = new List<UserInteraction>();
}

public enum ListingType
{
    Product,
    Service
}

public enum ListingStatus
{
    Active,
    Sold,
    Expired,
    Draft
}