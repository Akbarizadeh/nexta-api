namespace Nexa.Api.DTOs;

public record CreateOfferRequest(
    string Title,
    string? Description,
    string Category,
    List<string> Tags,
    string? ImageUrl,
    decimal? OriginalPrice,
    decimal? DiscountedPrice,
    int? DiscountPercent,
    double? Latitude,
    double? Longitude,
    DateTime StartDate,
    DateTime EndDate
);

public record OfferResponse(
    Guid Id,
    Guid BusinessId,
    string BusinessName,
    string Title,
    string? Description,
    string Category,
    List<string> Tags,
    string? ImageUrl,
    decimal? OriginalPrice,
    decimal? DiscountedPrice,
    int? DiscountPercent,
    double? Latitude,
    double? Longitude,
    DateTime StartDate,
    DateTime EndDate,
    int ViewCount,
    int LikeCount,
    int SaveCount,
    DateTime CreatedAt
);
