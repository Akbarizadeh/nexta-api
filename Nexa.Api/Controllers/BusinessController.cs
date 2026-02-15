using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexa.Api.Data;
using Nexa.Api.DTOs;
using Nexa.Api.Models;

namespace Nexa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessController : ControllerBase
{
    private readonly NexaDbContext _db;

    public BusinessController(NexaDbContext db)
    {
        _db = db;
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

        var businesses = await query.ToListAsync();

        // Apply distance filtering in memory if location provided  
        if (latitude.HasValue && longitude.HasValue)
        {
             radiusKm = radiusKm ?? 10;
            var filteredBusinesses = businesses
                .Where(b => b.Latitude.HasValue && b.Longitude.HasValue)
                .Select(b => new
                {
                    Business = b,
                    Distance = CalculateDistance(
                        latitude.Value, longitude.Value,
                        b.Latitude!.Value!, b.Longitude!.Value)
                })
                .Where(x => x.Distance <= radiusKm)
                .OrderBy(x => x.Distance)
                .Select(x => new BusinessResponse(
                    x.Business.Id, x.Business.UserId, x.Business.Name, x.Business.Description,
                    x.Business.LogoUrl, x.Business.CoverImageUrl, x.Business.Phone,
                    x.Business.Website, x.Business.Address,
                    x.Business.Latitude, x.Business.Longitude,
                    x.Business.Category, x.Business.IsVerified, x.Business.CreatedAt,
                    x.Business.Events.Count, x.Business.Listings.Count, x.Business.Offers.Count
                ))
                .ToList();

            return Ok(filteredBusinesses);
        }

        var response = businesses
            .Select(b => new BusinessResponse(
                b.Id, b.UserId, b.Name, b.Description,
                b.LogoUrl, b.CoverImageUrl, b.Phone, b.Website, b.Address,
                b.Latitude, b.Longitude,
                b.Category, b.IsVerified, b.CreatedAt,
                b.Events.Count, b.Listings.Count, b.Offers.Count
            ))
            .ToList();

        return Ok(response);
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
            b.Latitude, b.Longitude,
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
            Category = request.Category,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        _db.Businesses.Add(business);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBusiness), new { id = business.Id },
            new BusinessResponse(
                business.Id, business.UserId, business.Name, business.Description,
                business.LogoUrl, business.CoverImageUrl, business.Phone, business.Website, business.Address,
                business.Latitude, business.Longitude,
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