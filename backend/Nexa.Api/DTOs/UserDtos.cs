namespace Nexa.Api.DTOs;

public record CreateUserRequest(
    string Email,
    string DisplayName,
    List<string>? Interests,
    double? Latitude,
    double? Longitude
);

public record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    List<string> Interests,
    DateTime CreatedAt
);

public record InteractionRequest(
    string InteractionType,
    string ContentType,
    Guid ContentId
);

public record DiscoveryRequest(
    double Latitude,
    double Longitude,
    double? RadiusKm,
    string? Category,
    string? TimeFilter,
    string? SortBy,
    int Page = 1,
    int PageSize = 20
);

public record DiscoveryResponse(
    List<DiscoveryItem> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record DiscoveryItem(
    string ContentType,
    Guid Id,
    string Title,
    string? Description,
    string? ImageUrl,
    string Category,
    double? Latitude,
    double? Longitude,
    double DistanceKm,
    decimal? Price,
    int LikeCount,
    int SaveCount,
    DateTime CreatedAt,
    string? BusinessName
);
