namespace Nexa.Api.DTOs;

public record AiListingFromImageRequest(
    string ImageBase64
);

public record AiListingFromImageResponse(
    string Title,
    string Description,
    string Category,
    List<string> Tags,
    decimal PriceMin,
    decimal PriceMax,
    double ConfidenceScore
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
