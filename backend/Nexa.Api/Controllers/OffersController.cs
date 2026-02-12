using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Nexa.Api.Data;
using Nexa.Api.DTOs;
using Nexa.Api.Models;

namespace Nexa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OffersController : ControllerBase
{
    private readonly NexaDbContext _db;
    private readonly GeometryFactory _geometryFactory;

    public OffersController(NexaDbContext db, GeometryFactory geometryFactory)
    {
        _db = db;
        _geometryFactory = geometryFactory;
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

        if (latitude.HasValue && longitude.HasValue)
        {
            var userLocation = _geometryFactory.CreatePoint(
                new Coordinate(longitude.Value, latitude.Value));
            var radiusMeters = (radiusKm ?? 10) * 1000;

            query = query
                .Where(o => o.Location != null && o.Location.IsWithinDistance(userLocation, radiusMeters))
                .OrderBy(o => o.EndDate);
        }
        else
        {
            query = query.OrderBy(o => o.EndDate);
        }

        var offers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OfferResponse(
                o.Id, o.BusinessId, o.Business.Name,
                o.Title, o.Description, o.Category, o.Tags, o.ImageUrl,
                o.OriginalPrice, o.DiscountedPrice, o.DiscountPercent,
                o.Location != null ? o.Location.Y : null,
                o.Location != null ? o.Location.X : null,
                o.StartDate, o.EndDate, o.ViewCount,
                o.LikeCount, o.SaveCount, o.CreatedAt
            ))
            .ToListAsync();

        return Ok(offers);
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
            o.Location?.Y, o.Location?.X,
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
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            offer.Location = _geometryFactory.CreatePoint(
                new Coordinate(request.Longitude.Value, request.Latitude.Value));
        }

        _db.Offers.Add(offer);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOffer), new { id = offer.Id },
            new OfferResponse(
                offer.Id, offer.BusinessId, "Business",
                offer.Title, offer.Description, offer.Category, offer.Tags, offer.ImageUrl,
                offer.OriginalPrice, offer.DiscountedPrice, offer.DiscountPercent,
                offer.Location?.Y, offer.Location?.X,
                offer.StartDate, offer.EndDate, offer.ViewCount,
                offer.LikeCount, offer.SaveCount, offer.CreatedAt
            ));
    }
}
