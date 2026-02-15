namespace Nexa.Api.DTOs;

public record CreateListingRequest(
    string Title,
    string? Description,
    string Category,
    List<string> Tags,
    List<string> ImageUrls,
    decimal? Price,
    decimal? PriceMin,
    decimal? PriceMax,
    string Type,
    double? Latitude,
    double? Longitude
);

public record UpdateListingRequest(
    string? Title,
    string? Description,
    string? Category,
    List<string>? Tags,
    decimal? Price,
    decimal? PriceMin,
    decimal? PriceMax,
    string? Status
);

public record ListingResponse(
    Guid Id,
    Guid SellerId,
    string SellerName,
    Guid? BusinessId,
    string? BusinessName,
    string Title,
    string? Description,
    string Category,
    List<string> Tags,
    List<string> ImageUrls,
    decimal? PriceMin,
    decimal? PriceMax,
    decimal? Price,
    string Type,
    string Status,
    double? Latitude,
    double? Longitude,
    double? AiConfidenceScore,
    int ViewCount,
    int LikeCount,
    int SaveCount,
    DateTime CreatedAt
);

public record ListingSearchRequest(
    string? Query,
    double? Latitude,
    double? Longitude,
    double? RadiusKm,
    string? Category,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? SortBy,
    int Page = 1,
    int PageSize = 20
);
