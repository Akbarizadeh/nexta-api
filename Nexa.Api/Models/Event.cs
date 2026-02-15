namespace Nexa.Api.Models;

public class Event
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string? ImageUrl { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Price { get; set; }
    public bool IsFree { get; set; }
    public int? MaxAttendees { get; set; }
    public int AttendeeCount { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int SaveCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Business Business { get; set; } = null!;
    public ICollection<UserInteraction> Interactions { get; set; } = new List<UserInteraction>();
}