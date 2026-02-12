using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Nexa.Api.Data;
using Nexa.Api.DTOs;
using Nexa.Api.Models;

namespace Nexa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly NexaDbContext _db;
    private readonly GeometryFactory _geometryFactory;

    public EventsController(NexaDbContext db, GeometryFactory geometryFactory)
    {
        _db = db;
        _geometryFactory = geometryFactory;
    }

    [HttpGet]
    public async Task<ActionResult<List<EventResponse>>> GetEvents(
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] double? radiusKm,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Events
            .Include(e => e.Business)
            .Where(e => e.EndDate == null || e.EndDate > DateTime.UtcNow)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        if (latitude.HasValue && longitude.HasValue)
        {
            var userLocation = _geometryFactory.CreatePoint(
                new Coordinate(longitude.Value, latitude.Value));
            var radiusMeters = (radiusKm ?? 10) * 1000;

            query = query
                .Where(e => e.Location != null && e.Location.IsWithinDistance(userLocation, radiusMeters))
                .OrderBy(e => e.StartDate);
        }
        else
        {
            query = query.OrderBy(e => e.StartDate);
        }

        var events = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EventResponse(
                e.Id, e.BusinessId, e.Business.Name,
                e.Title, e.Description, e.Category, e.Tags, e.ImageUrl,
                e.Address, e.Location != null ? e.Location.Y : null,
                e.Location != null ? e.Location.X : null,
                e.StartDate, e.EndDate, e.Price, e.IsFree,
                e.MaxAttendees, e.AttendeeCount, e.ViewCount,
                e.LikeCount, e.SaveCount, e.CreatedAt
            ))
            .ToListAsync();

        return Ok(events);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EventResponse>> GetEvent(Guid id)
    {
        var e = await _db.Events
            .Include(e => e.Business)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (e == null) return NotFound();

        e.ViewCount++;
        await _db.SaveChangesAsync();

        return Ok(new EventResponse(
            e.Id, e.BusinessId, e.Business.Name,
            e.Title, e.Description, e.Category, e.Tags, e.ImageUrl,
            e.Address, e.Location?.Y, e.Location?.X,
            e.StartDate, e.EndDate, e.Price, e.IsFree,
            e.MaxAttendees, e.AttendeeCount, e.ViewCount,
            e.LikeCount, e.SaveCount, e.CreatedAt
        ));
    }

    [HttpPost]
    public async Task<ActionResult<EventResponse>> CreateEvent(CreateEventRequest request)
    {
        var ev = new Event
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(), // TODO: get from auth
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Tags = request.Tags,
            ImageUrl = request.ImageUrl,
            Address = request.Address,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Price = request.Price,
            IsFree = request.IsFree,
            MaxAttendees = request.MaxAttendees
        };

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            ev.Location = _geometryFactory.CreatePoint(
                new Coordinate(request.Longitude.Value, request.Latitude.Value));
        }

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.Id },
            new EventResponse(
                ev.Id, ev.BusinessId, "Business",
                ev.Title, ev.Description, ev.Category, ev.Tags, ev.ImageUrl,
                ev.Address, ev.Location?.Y, ev.Location?.X,
                ev.StartDate, ev.EndDate, ev.Price, ev.IsFree,
                ev.MaxAttendees, ev.AttendeeCount, ev.ViewCount,
                ev.LikeCount, ev.SaveCount, ev.CreatedAt
            ));
    }
}
