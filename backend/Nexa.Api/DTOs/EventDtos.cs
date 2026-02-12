namespace Nexa.Api.DTOs;

public record CreateEventRequest(
    string Title,
    string? Description,
    string Category,
    List<string> Tags,
    string? ImageUrl,
    string? Address,
    double? Latitude,
    double? Longitude,
    DateTime StartDate,
    DateTime? EndDate,
    decimal? Price,
    bool IsFree,
    int? MaxAttendees
);

public record EventResponse(
    Guid Id,
    Guid BusinessId,
    string BusinessName,
    string Title,
    string? Description,
    string Category,
    List<string> Tags,
    string? ImageUrl,
    string? Address,
    double? Latitude,
    double? Longitude,
    DateTime StartDate,
    DateTime? EndDate,
    decimal? Price,
    bool IsFree,
    int? MaxAttendees,
    int AttendeeCount,
    int ViewCount,
    int LikeCount,
    int SaveCount,
    DateTime CreatedAt
);
