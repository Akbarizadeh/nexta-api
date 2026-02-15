using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexa.Api.Data;
using Nexa.Api.DTOs;
using Nexa.Api.Models;

namespace Nexa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListingsController : ControllerBase
{
    private readonly NexaDbContext _db;

    public ListingsController(NexaDbContext db)
    {
        _db = db;
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

        var listings = await query.ToListAsync();

        // Apply distance filtering in memory if location provided  
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            var radiusKm = request.RadiusKm ?? 10;
            var filteredListings = listings
                .Where(l => l.Latitude.HasValue && l.Longitude.HasValue)
                .Select(l => new
                {
                    Listing = l,
                    Distance = CalculateDistance(
                        request.Latitude.Value, request.Longitude.Value,
                        l.Latitude.Value, l.Longitude.Value)
                })
                .Where(x => x.Distance <= radiusKm)
                .OrderBy(x => x.Distance)
                .Select(x => x.Listing)
                .ToList();

            listings = filteredListings;
        }
        else
        {
            listings = listings.OrderByDescending(l => l.CreatedAt).ToList();
        }

        var result = listings
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new ListingResponse(
                l.Id, l.SellerId, l.Seller.DisplayName,
                l.BusinessId, l.Business != null ? l.Business.Name : null,
                l.Title, l.Description, l.Category, l.Tags, l.ImageUrls,
                l.PriceMin, l.PriceMax, l.Price,
                l.Type.ToString(), l.Status.ToString(),
                l.Latitude, l.Longitude,
                l.AiConfidenceScore, l.ViewCount, l.LikeCount, l.SaveCount, l.CreatedAt
            ))
            .ToList();

        return Ok(result);
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
            l.Latitude, l.Longitude,
            l.AiConfidenceScore, l.ViewCount, l.LikeCount, l.SaveCount, l.CreatedAt
        ));
    }

    [HttpPost]
    public async Task<ActionResult<ListingResponse>> CreateListing([FromBody] CreateListingRequest request)
    {
        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.Parse("11111111-1111-1111-1111-111111111111"), // Test Seller 1  
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Tags = request.Tags,
            ImageUrls = request.ImageUrls,
            Price = request.Price,
            PriceMin = request.PriceMin,
            PriceMax = request.PriceMax,
            Type = Enum.Parse<ListingType>(request.Type, true),
            Status = ListingStatus.Active,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        _db.Listings.Add(listing);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetListing), new { id = listing.Id },
            new ListingResponse(
                listing.Id, listing.SellerId, "User",
                listing.BusinessId, null,
                listing.Title, listing.Description, listing.Category, listing.Tags, listing.ImageUrls,
                listing.PriceMin, listing.PriceMax, listing.Price,
                listing.Type.ToString(), listing.Status.ToString(),
                listing.Latitude, listing.Longitude,
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