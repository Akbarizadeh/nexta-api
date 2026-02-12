using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Nexa.Api.Data;
using Nexa.Api.DTOs;

namespace Nexa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscoveryController : ControllerBase
{
    private readonly NexaDbContext _db;
    private readonly GeometryFactory _geometryFactory;

    public DiscoveryController(NexaDbContext db, GeometryFactory geometryFactory)
    {
        _db = db;
        _geometryFactory = geometryFactory;
    }

    [HttpGet]
    public async Task<ActionResult<DiscoveryResponse>> Discover([FromQuery] DiscoveryRequest request)
    {
        var items = new List<DiscoveryItem>();
        Point? userLocation = null;

        if (request.Latitude != 0 && request.Longitude != 0)
        {
            userLocation = _geometryFactory.CreatePoint(
                new Coordinate(request.Longitude, request.Latitude));
        }

        var radiusMeters = (request.RadiusKm ?? 10) * 1000;

        var listings = await _db.Listings
            .Include(l => l.Business)
            .Where(l => l.Status == Models.ListingStatus.Active)
            .Where(l => string.IsNullOrEmpty(request.Category) || l.Category == request.Category)
            .Where(l => userLocation == null || (l.Location != null && l.Location.IsWithinDistance(userLocation, radiusMeters)))
            .OrderByDescending(l => l.CreatedAt)
            .Take(request.PageSize)
            .ToListAsync();

        foreach (var l in listings)
        {
            var dist = userLocation != null && l.Location != null
                ? l.Location.Distance(userLocation) / 1000.0
                : 0;

            items.Add(new DiscoveryItem(
                "Listing", l.Id, l.Title, l.Description,
                l.ImageUrls.FirstOrDefault(), l.Category,
                l.Location?.Y, l.Location?.X, dist,
                l.Price ?? l.PriceMin, l.LikeCount, l.SaveCount,
                l.CreatedAt, l.Business?.Name
            ));
        }

        var events = await _db.Events
            .Include(e => e.Business)
            .Where(e => e.EndDate == null || e.EndDate > DateTime.UtcNow)
            .Where(e => string.IsNullOrEmpty(request.Category) || e.Category == request.Category)
            .Where(e => userLocation == null || (e.Location != null && e.Location.IsWithinDistance(userLocation, radiusMeters)))
            .OrderBy(e => e.StartDate)
            .Take(request.PageSize)
            .ToListAsync();

        foreach (var e in events)
        {
            var dist = userLocation != null && e.Location != null
                ? e.Location.Distance(userLocation) / 1000.0
                : 0;

            items.Add(new DiscoveryItem(
                "Event", e.Id, e.Title, e.Description,
                e.ImageUrl, e.Category,
                e.Location?.Y, e.Location?.X, dist,
                e.Price, e.LikeCount, e.SaveCount,
                e.CreatedAt, e.Business.Name
            ));
        }

        var offers = await _db.Offers
            .Include(o => o.Business)
            .Where(o => o.EndDate > DateTime.UtcNow)
            .Where(o => string.IsNullOrEmpty(request.Category) || o.Category == request.Category)
            .Where(o => userLocation == null || (o.Location != null && o.Location.IsWithinDistance(userLocation, radiusMeters)))
            .OrderBy(o => o.EndDate)
            .Take(request.PageSize)
            .ToListAsync();

        foreach (var o in offers)
        {
            var dist = userLocation != null && o.Location != null
                ? o.Location.Distance(userLocation) / 1000.0
                : 0;

            items.Add(new DiscoveryItem(
                "Offer", o.Id, o.Title, o.Description,
                o.ImageUrl, o.Category,
                o.Location?.Y, o.Location?.X, dist,
                o.DiscountedPrice, o.LikeCount, o.SaveCount,
                o.CreatedAt, o.Business.Name
            ));
        }

        var sorted = request.SortBy?.ToLower() switch
        {
            "distance" => items.OrderBy(i => i.DistanceKm).ToList(),
            "popular" => items.OrderByDescending(i => i.LikeCount + i.SaveCount).ToList(),
            _ => items.OrderByDescending(i => i.CreatedAt).ToList()
        };

        var paged = sorted
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Ok(new DiscoveryResponse(paged, sorted.Count, request.Page, request.PageSize));
    }
}
