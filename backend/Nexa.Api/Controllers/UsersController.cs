using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Nexa.Api.Data;
using Nexa.Api.DTOs;
using Nexa.Api.Models;

namespace Nexa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly NexaDbContext _db;
    private readonly GeometryFactory _geometryFactory;

    public UsersController(NexaDbContext db, GeometryFactory geometryFactory)
    {
        _db = db;
        _geometryFactory = geometryFactory;
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser(CreateUserRequest request)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            DisplayName = request.DisplayName,
            Interests = request.Interests ?? new List<string>()
        };

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            user.Location = _geometryFactory.CreatePoint(
                new Coordinate(request.Longitude.Value, request.Latitude.Value));
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id },
            new UserResponse(user.Id, user.Email, user.DisplayName, user.AvatarUrl,
                user.Role.ToString(), user.Interests, user.CreatedAt));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetUser(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        return Ok(new UserResponse(user.Id, user.Email, user.DisplayName, user.AvatarUrl,
            user.Role.ToString(), user.Interests, user.CreatedAt));
    }

    [HttpPost("interactions")]
    public async Task<ActionResult> AddInteraction(InteractionRequest request)
    {
        var userId = Guid.NewGuid(); // TODO: get from auth

        var interaction = new UserInteraction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = Enum.Parse<InteractionType>(request.InteractionType, true),
            ContentType = Enum.Parse<Models.ContentType>(request.ContentType, true),
            ContentId = request.ContentId
        };

        _db.UserInteractions.Add(interaction);

        switch (interaction.ContentType)
        {
            case Models.ContentType.Listing:
                var listing = await _db.Listings.FindAsync(interaction.ContentId);
                if (listing != null)
                {
                    if (interaction.Type == InteractionType.Like) listing.LikeCount++;
                    if (interaction.Type == InteractionType.Save) listing.SaveCount++;
                }
                break;
            case Models.ContentType.Event:
                var ev = await _db.Events.FindAsync(interaction.ContentId);
                if (ev != null)
                {
                    if (interaction.Type == InteractionType.Like) ev.LikeCount++;
                    if (interaction.Type == InteractionType.Save) ev.SaveCount++;
                    if (interaction.Type == InteractionType.Attend) ev.AttendeeCount++;
                }
                break;
            case Models.ContentType.Offer:
                var offer = await _db.Offers.FindAsync(interaction.ContentId);
                if (offer != null)
                {
                    if (interaction.Type == InteractionType.Like) offer.LikeCount++;
                    if (interaction.Type == InteractionType.Save) offer.SaveCount++;
                }
                break;
        }

        await _db.SaveChangesAsync();
        return Ok();
    }
}
