namespace Nexa.Api.DTOs;

public record CreateBusinessRequest(
    string Name,
    string? Description,
    string? Phone,
    string? Website,
    string? Address,
    double? Latitude,
    double? Longitude,
    string? Category
);

public record BusinessResponse(
    Guid Id,
    Guid UserId,
    string Name,
    string? Description,
    string? LogoUrl,
    string? CoverImageUrl,
    string? Phone,
    string? Website,
    string? Address,
    double? Latitude,
    double? Longitude,
    string? Category,
    bool IsVerified,
    DateTime CreatedAt,
    int EventCount,
    int ListingCount,
    int OfferCount
);

public record BusinessAnalyticsResponse(
    Guid BusinessId,
    int TotalViews,
    int TotalLikes,
    int TotalSaves,
    int TotalListings,
    int TotalEvents,
    int TotalOffers
);
