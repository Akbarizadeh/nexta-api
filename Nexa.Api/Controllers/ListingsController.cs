using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Nexa.Api.Data;
using Nexa.Api.DTOs;
using Nexa.Api.Models;

namespace Nexa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListingsController : ControllerBase
{
    private readonly NexaDbContext _db;
    private readonly GeometryFactory _geometryFactory;

    public ListingsController(NexaDbContext db, GeometryFactory geometryFactory)
    {
        _db = db;
        _geometryFactory = geometryFactory;
    }

    [HttpGet]
    public async Task<ActionResult<List<ListingResponse>>> GetListings([FromQuery] ListingSearchRequest request)
    {
        var query = _db.Listings
            .Include(l => l.Seller)
            .Include(l => l.Business)
            .Where(l => l.Status == ListingStatus.Active)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(l => l.Category == request.Category);

        if (request.MinPrice.HasValue)
            query = query.Where(l => l.PriceMax >= request.MinPrice.Value);

        if (request.MaxPrice.HasValue)
            query = query.Where(l => l.PriceMin <= request.MaxPrice.Value);

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            var userLocation = _geometryFactory.CreatePoint(
                new Coordinate(request.Longitude.Value, request.Latitude.Value));
            var radiusMeters = (request.RadiusKm ?? 10) * 1000;

            query = query
                .Where(l => l.Location != null && l.Location.IsWithinDistance(userLocation, radiusMeters))
                .OrderBy(l => l.Location!.Distance(userLocation));
        }
        else
        {
            query = query.OrderByDescending(l => l.CreatedAt);
        }

        var listings = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new ListingResponse(
                l.Id, l.SellerId, l.Seller.DisplayName,
                l.BusinessId, l.Business != null ? l.Business.Name : null,
                l.Title, l.Description, l.Category, l.Tags, l.ImageUrls,
                l.PriceMin, l.PriceMax, l.Price,
                l.Type.ToString(), l.Status.ToString(),
                l.Location != null ? l.Location.Y : null,
                l.Location != null ? l.Location.X : null,
                l.AiConfidenceScore, l.ViewCount, l.LikeCount, l.SaveCount, l.CreatedAt
            ))
            .ToListAsync();

        return Ok(listings);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ListingResponse>> GetListing(Guid id)
    {
        var l = await _db.Listings
            .Include(l => l.Seller)
            .Include(l => l.Business)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (l == null) return NotFound();

        l.ViewCount++;
        await _db.SaveChangesAsync();

        return Ok(new ListingResponse(
            l.Id, l.SellerId, l.Seller.DisplayName,
            l.BusinessId, l.Business?.Name,
            l.Title, l.Description, l.Category, l.Tags, l.ImageUrls,
            l.PriceMin, l.PriceMax, l.Price,
            l.Type.ToString(), l.Status.ToString(),
            l.Location?.Y, l.Location?.X,
            l.AiConfidenceScore, l.ViewCount, l.LikeCount, l.SaveCount, l.CreatedAt
        ));
    }

    [HttpPost]
    public async Task<ActionResult<ListingResponse>> CreateListing(CreateListingRequest request)
    {
        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(), // TODO: get from auth
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Tags = request.Tags,
            ImageUrls = request.ImageUrls,
            Price = request.Price,
            PriceMin = request.PriceMin,
            PriceMax = request.PriceMax,
            Type = Enum.Parse<ListingType>(request.Type, true),
            Status = ListingStatus.Active
        };

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            listing.Location = _geometryFactory.CreatePoint(
                new Coordinate(request.Longitude.Value, request.Latitude.Value));
        }

        _db.Listings.Add(listing);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetListing), new { id = listing.Id },
            new ListingResponse(
                listing.Id, listing.SellerId, "User",
                listing.BusinessId, null,
                listing.Title, listing.Description, listing.Category, listing.Tags, listing.ImageUrls,
                listing.PriceMin, listing.PriceMax, listing.Price,
                listing.Type.ToString(), listing.Status.ToString(),
                listing.Location?.Y, listing.Location?.X,
                listing.AiConfidenceScore, listing.ViewCount, listing.LikeCount, listing.SaveCount,
                listing.CreatedAt
            ));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateListing(Guid id, UpdateListingRequest request)
    {
        var listing = await _db.Listings.FindAsync(id);
        if (listing == null) return NotFound();

        if (request.Title != null) listing.Title = request.Title;
        if (request.Description != null) listing.Description = request.Description;
        if (request.Category != null) listing.Category = request.Category;
        if (request.Tags != null) listing.Tags = request.Tags;
        if (request.Price.HasValue) listing.Price = request.Price.Value;
        if (request.PriceMin.HasValue) listing.PriceMin = request.PriceMin.Value;
        if (request.PriceMax.HasValue) listing.PriceMax = request.PriceMax.Value;
        if (request.Status != null) listing.Status = Enum.Parse<ListingStatus>(request.Status, true);

        listing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteListing(Guid id)
    {
        var listing = await _db.Listings.FindAsync(id);
        if (listing == null) return NotFound();

        _db.Listings.Remove(listing);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
