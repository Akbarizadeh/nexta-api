namespace Nexa.Api.DTOs;

public class AiVisionResult
{
    public string InterpretedIntent { get; set; }
    public string SuggestedCategory { get; set; }
    public PriceRange PriceRange { get; set; }
    public List<string> Keywords { get; set; }
    public List<AiListingDraft> ListingDrafts { get; set; }
}

public class PriceRange
{
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
}

public class AiListingDraft
{

    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public List<string> Tags { get; set; }
    public decimal PriceMin { get; set; }
    public decimal PriceMax { get; set; }
    public double ConfidenceScore { get; set; }
}
public record AiListingFromImageRequest(
    string ImageBase64
);

public record AiListingFromImageResponse(
    string Title,
    string Description,
    string Category,
    List<string> Tags,
    decimal? PriceMin,
    decimal? PriceMax,
    double ConfidenceScore,
    string InterpretedIntent   // 👈 خیلی مهم و کاربردی
);

public record AiRecommendRequest(
    Guid UserId,
    double Latitude,
    double Longitude,
    List<string>? Interests,
    string? TimeContext
);

public record AiRecommendResponse(
    List<RecommendedItem> Items
);

public record RecommendedItem(
    string ContentType,
    Guid ContentId,
    string Title,
    string? Description,
    string? ImageUrl,
    string Category,
    double RelevanceScore,
    double DistanceKm
);

public record AiSearchRequest(
    string Query,
    double Latitude,
    double Longitude,
    double? RadiusKm,
    string? Category,
    decimal? MinPrice,
    decimal? MaxPrice
);

public record AiSearchResponse(
    string InterpretedIntent,
    List<RecommendedItem> Results
);
