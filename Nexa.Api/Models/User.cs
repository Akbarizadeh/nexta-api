namespace Nexa.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Normal;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<string> Interests { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Business? Business { get; set; }
    public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    public ICollection<UserInteraction> Interactions { get; set; } = new List<UserInteraction>();
}

public enum UserRole
{
    Normal,
    Seller,
    Business
}