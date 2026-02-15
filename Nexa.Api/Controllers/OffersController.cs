using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexa.Api.Data;
using Nexa.Api.DTOs;
using Nexa.Api.Models;

namespace Nexa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OffersController : ControllerBase
{
    private readonly NexaDbContext _db;

    public OffersController(NexaDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<OfferResponse>>> GetOffers(
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] double? radiusKm,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Offers
            .Include(o => o.Business)
            .Where(o => o.EndDate > DateTime.UtcNow)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(o => o.Category == category);

        var offers = await query.OrderBy(o => o.EndDate).ToListAsync();

        // Apply distance filtering in memory if location provided  
        if (latitude.HasValue && longitude.HasValue)
        {
            var radius = radiusKm ?? 10;
            var filteredOffers = offers
                .Where(o => o.Latitude.HasValue && o.Longitude.HasValue)
                .Select(o => new
                {
                    Offer = o,
                    Distance = CalculateDistance(
                        latitude.Value, longitude.Value,
                        o.Latitude.Value, o.Longitude.Value)
                })
                .Where(x => x.Distance <= radius)
                .OrderBy(x => x.Offer.EndDate)
                .Select(x => x.Offer)
                .ToList();

            offers = filteredOffers;
        }

        var result = offers
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OfferResponse(
                o.Id, o.BusinessId, o.Business.Name,
                o.Title, o.Description, o.Category, o.Tags, o.ImageUrl,
                o.OriginalPrice, o.DiscountedPrice, o.DiscountPercent,
                o.Latitude, o.Longitude,
                o.StartDate, o.EndDate, o.ViewCount,
                o.LikeCount, o.SaveCount, o.CreatedAt
            ))
            .ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OfferResponse>> GetOffer(Guid id)
    {
        var o = await _db.Offers
            .Include(o => o.Business)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (o == null) return NotFound();

        o.ViewCount++;
        await _db.SaveChangesAsync();

        return Ok(new OfferResponse(
            o.Id, o.BusinessId, o.Business.Name,
            o.Title, o.Description, o.Category, o.Tags, o.ImageUrl,
            o.OriginalPrice, o.DiscountedPrice, o.DiscountPercent,
            o.Latitude, o.Longitude,
            o.StartDate, o.EndDate, o.ViewCount,
            o.LikeCount, o.SaveCount, o.CreatedAt
        ));
    }

    [HttpPost]
    public async Task<ActionResult<OfferResponse>> CreateOffer(CreateOfferRequest request)
    {
        var offer = new Offer
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(), // TODO: get from auth  
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Tags = request.Tags,
            ImageUrl = request.ImageUrl,
            OriginalPrice = request.OriginalPrice,
            DiscountedPrice = request.DiscountedPrice,
            DiscountPercent = request.DiscountPercent,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        _db.Offers.Add(offer);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOffer), new { id = offer.Id },
            new OfferResponse(
                offer.Id, offer.BusinessId, "Business",
                offer.Title, offer.Description, offer.Category, offer.Tags, offer.ImageUrl,
                offer.OriginalPrice, offer.DiscountedPrice, offer.DiscountPercent,
                offer.Latitude, offer.Longitude,
                offer.StartDate, offer.EndDate, offer.ViewCount,
                offer.LikeCount, offer.SaveCount, offer.CreatedAt
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