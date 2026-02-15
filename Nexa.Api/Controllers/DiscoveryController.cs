using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexa.Api.Data;
using Nexa.Api.DTOs;

namespace Nexa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscoveryController : ControllerBase
{
    private readonly NexaDbContext _db;

    public DiscoveryController(NexaDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<DiscoveryResponse>> Discover([FromQuery] DiscoveryRequest request)
    {
        var items = new List<DiscoveryItem>();
        var radiusKm = request.RadiusKm ?? 10;

        // Get all listings  
        var listings = await _db.Listings
            .Include(l => l.Business)
            .Where(l => l.Status == Models.ListingStatus.Active)
            .Where(l => string.IsNullOrEmpty(request.Category) || l.Category == request.Category)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        // Apply distance filtering in memory if location provided  
        if (request.Latitude != 0 && request.Longitude != 0)
        {
            var filteredListings = listings
                .Where(l => (l.Latitude == null || l.Longitude == null) || l.Latitude.HasValue && l.Longitude.HasValue)
                .Select(l => new
                {
                    Listing = l,
                    //Distance = CalculateDistance(
                    //    request.Latitude, request.Longitude,
                    //  l.Longitude == null ? 0 : l.Latitude!.Value, l.Longitude == null ? 0 : l.Longitude.Value)
                })
                //.Where(x => 1 ==1 || x.Distance <= radiusKm)
                //.OrderBy(x => x.Distance)
                .Take(request.PageSize)
                .Select(x => x.Listing)
                .ToList();

            listings = filteredListings;
        }
        else
        {
            listings = listings.Take(request.PageSize).ToList();
        }

        foreach (var l in listings)
        {
            var dist = (request.Latitude != 0 && request.Longitude != 0 && l.Latitude.HasValue && l.Longitude.HasValue)
                ? CalculateDistance(request.Latitude, request.Longitude, l.Latitude.Value, l.Longitude.Value)
                : 0;

            items.Add(new DiscoveryItem(
                "Listing", l.Id, l.Title, l.Description,
                l.ImageUrls.FirstOrDefault(), l.Category,
                l.Latitude, l.Longitude, dist,
                l.Price ?? l.PriceMin, l.LikeCount, l.SaveCount,
                l.CreatedAt, l.Business?.Name
            ));
        }

        // Get all events  
        var events = await _db.Events
            .Include(e => e.Business)
            .Where(e => e.EndDate == null || e.EndDate > DateTime.UtcNow)
            .Where(e => string.IsNullOrEmpty(request.Category) || e.Category == request.Category)
            .OrderBy(e => e.StartDate)
            .ToListAsync();

        // Apply distance filtering in memory if location provided  
        if (request.Latitude != 0 && request.Longitude != 0)
        {
            var filteredEvents = events
                .Where(e => e.Latitude.HasValue && e.Longitude.HasValue)
                .Select(e => new
                {
                    Event = e,
                    Distance = CalculateDistance(
                        request.Latitude, request.Longitude,
                        e.Latitude.Value, e.Longitude.Value)
                })
                .Where(x => x.Distance <= radiusKm)
                .OrderBy(x => x.Event.StartDate)
                .Take(request.PageSize)
                .Select(x => x.Event)
                .ToList();

            events = filteredEvents;
        }
        else
        {
            events = events.Take(request.PageSize).ToList();
        }

        foreach (var e in events)
        {
            var dist = (request.Latitude != 0 && request.Longitude != 0 && e.Latitude.HasValue && e.Longitude.HasValue)
                ? CalculateDistance(request.Latitude, request.Longitude, e.Latitude.Value, e.Longitude.Value)
                : 0;

            items.Add(new DiscoveryItem(
                "Event", e.Id, e.Title, e.Description,
                e.ImageUrl, e.Category,
                e.Latitude, e.Longitude, dist,
                e.Price, e.LikeCount, e.SaveCount,
                e.CreatedAt, e.Business.Name
            ));
        }

        // Get all offers  
        var offers = await _db.Offers
            .Include(o => o.Business)
            .Where(o => o.EndDate > DateTime.UtcNow)
            .Where(o => string.IsNullOrEmpty(request.Category) || o.Category == request.Category)
            .OrderBy(o => o.EndDate)
            .ToListAsync();

        // Apply distance filtering in memory if location provided  
        if (request.Latitude != 0 && request.Longitude != 0)
        {
            var filteredOffers = offers
                .Where(o => o.Latitude.HasValue && o.Longitude.HasValue)
                .Select(o => new
                {
                    Offer = o,
                    Distance = CalculateDistance(
                        request.Latitude, request.Longitude,
                        o.Latitude.Value, o.Longitude.Value)
                })
                .Where(x => x.Distance <= radiusKm)
                .OrderBy(x => x.Offer.EndDate)
                .Take(request.PageSize)
                .Select(x => x.Offer)
                .ToList();

            offers = filteredOffers;
        }
        else
        {
            offers = offers.Take(request.PageSize).ToList();
        }

        foreach (var o in offers)
        {
            var dist = (request.Latitude != 0 && request.Longitude != 0 && o.Latitude.HasValue && o.Longitude.HasValue)
                ? CalculateDistance(request.Latitude, request.Longitude, o.Latitude.Value, o.Longitude.Value)
                : 0;

            items.Add(new DiscoveryItem(
                "Offer", o.Id, o.Title, o.Description,
                o.ImageUrl, o.Category,
                o.Latitude, o.Longitude, dist,
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