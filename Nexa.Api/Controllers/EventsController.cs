using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexa.Api.Data;
using Nexa.Api.DTOs;
using Nexa.Api.Models;

namespace Nexa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly NexaDbContext _db;

    public EventsController(NexaDbContext db)
    {
        _db = db;
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

        var events = await query.OrderBy(e => e.StartDate).ToListAsync();

        // Apply distance filtering in memory if location provided  
        if (latitude.HasValue && longitude.HasValue)
        {
            var radius = radiusKm ?? 10;
            var filteredEvents = events
                .Where(e => e.Latitude.HasValue && e.Longitude.HasValue)
                .Select(e => new
                {
                    Event = e,
                    Distance = CalculateDistance(
                        latitude.Value, longitude.Value,
                        e.Latitude.Value, e.Longitude.Value)
                })
                .Where(x => x.Distance <= radius)
                .OrderBy(x => x.Event.StartDate)
                .Select(x => x.Event)
                .ToList();

            events = filteredEvents;
        }

        var result = events
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EventResponse(
                e.Id, e.BusinessId, e.Business.Name,
                e.Title, e.Description, e.Category, e.Tags, e.ImageUrl,
                e.Address, e.Latitude, e.Longitude,
                e.StartDate, e.EndDate, e.Price, e.IsFree,
                e.MaxAttendees, e.AttendeeCount, e.ViewCount,
                e.LikeCount, e.SaveCount, e.CreatedAt
            ))
            .ToList();

        return Ok(result);
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
            e.Address, e.Latitude, e.Longitude,
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
            MaxAttendees = request.MaxAttendees,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.Id },
            new EventResponse(
                ev.Id, ev.BusinessId, "Business",
                ev.Title, ev.Description, ev.Category, ev.Tags, ev.ImageUrl,
                ev.Address, ev.Latitude, ev.Longitude,
                ev.StartDate, ev.EndDate, ev.Price, ev.IsFree,
                ev.MaxAttendees, ev.AttendeeCount, ev.ViewCount,
                ev.LikeCount, ev.SaveCount, ev.CreatedAt
            ));
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371; // Earth radius in km  
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}