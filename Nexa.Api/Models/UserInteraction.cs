namespace Nexa.Api.Models;

public class UserInteraction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public InteractionType Type { get; set; }
    public ContentType ContentType { get; set; }
    public Guid ContentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}

public enum InteractionType
{
    Like,
    Save,
    Attend,
    View
}

public enum ContentType
{
    Listing,
    Event,
    Offer
}
