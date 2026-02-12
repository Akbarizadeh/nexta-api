using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Nexa.Api.Data;
using Nexa.Api.DTOs;
using Nexa.Api.Models;

namespace Nexa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessController : ControllerBase
{
    private readonly NexaDbContext _db;
    private readonly GeometryFactory _geometryFactory;

    public BusinessController(NexaDbContext db, GeometryFactory geometryFactory)
    {
        _db = db;
        _geometryFactory = geometryFactory;
    }

    [HttpGet]
    public async Task<ActionResult<List<BusinessResponse>>> GetBusinesses(
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] double? radiusKm,
        [FromQuery] string? category)
    {
        var query = _db.Businesses
            .Include(b => b.Events)
            .Include(b => b.Listings)
            .Include(b => b.Offers)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(b => b.Category == category);

        if (latitude.HasValue && longitude.HasValue)
        {
            var userLocation = _geometryFactory.CreatePoint(
                new Coordinate(longitude.Value, latitude.Value));
            var radiusMeters = (radiusKm ?? 10) * 1000;

            query = query
                .Where(b => b.Location != null && b.Location.IsWithinDistance(userLocation, radiusMeters));
        }

        var businesses = await query
            .Select(b => new BusinessResponse(
                b.Id, b.UserId, b.Name, b.Description,
                b.LogoUrl, b.CoverImageUrl, b.Phone, b.Website, b.Address,
                b.Location != null ? b.Location.Y : null,
                b.Location != null ? b.Location.X : null,
                b.Category, b.IsVerified, b.CreatedAt,
                b.Events.Count, b.Listings.Count, b.Offers.Count
            ))
            .ToListAsync();

        return Ok(businesses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BusinessResponse>> GetBusiness(Guid id)
    {
        var b = await _db.Businesses
            .Include(b => b.Events)
            .Include(b => b.Listings)
            .Include(b => b.Offers)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (b == null) return NotFound();

        return Ok(new BusinessResponse(
            b.Id, b.UserId, b.Name, b.Description,
            b.LogoUrl, b.CoverImageUrl, b.Phone, b.Website, b.Address,
            b.Location?.Y, b.Location?.X,
            b.Category, b.IsVerified, b.CreatedAt,
            b.Events.Count, b.Listings.Count, b.Offers.Count
        ));
    }

    [HttpPost]
    public async Task<ActionResult<BusinessResponse>> CreateBusiness(CreateBusinessRequest request)
    {
        var business = new Business
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(), // TODO: get from auth
            Name = request.Name,
            Description = request.Description,
            Phone = request.Phone,
            Website = request.Website,
            Address = request.Address,
            Category = request.Category
        };

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            business.Location = _geometryFactory.CreatePoint(
                new Coordinate(request.Longitude.Value, request.Latitude.Value));
        }

        _db.Businesses.Add(business);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBusiness), new { id = business.Id },
            new BusinessResponse(
                business.Id, business.UserId, business.Name, business.Description,
                business.LogoUrl, business.CoverImageUrl, business.Phone, business.Website, business.Address,
                business.Location?.Y, business.Location?.X,
                business.Category, business.IsVerified, business.CreatedAt,
                0, 0, 0
            ));
    }

    [HttpGet("{id}/analytics")]
    public async Task<ActionResult<BusinessAnalyticsResponse>> GetAnalytics(Guid id)
    {
        var business = await _db.Businesses
            .Include(b => b.Events)
            .Include(b => b.Listings)
            .Include(b => b.Offers)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (business == null) return NotFound();

        return Ok(new BusinessAnalyticsResponse(
            business.Id,
            business.Listings.Sum(l => l.ViewCount) + business.Events.Sum(e => e.ViewCount) + business.Offers.Sum(o => o.ViewCount),
            business.Listings.Sum(l => l.LikeCount) + business.Events.Sum(e => e.LikeCount) + business.Offers.Sum(o => o.LikeCount),
            business.Listings.Sum(l => l.SaveCount) + business.Events.Sum(e => e.SaveCount) + business.Offers.Sum(o => o.SaveCount),
            business.Listings.Count,
            business.Events.Count,
            business.Offers.Count
        ));
    }
}
