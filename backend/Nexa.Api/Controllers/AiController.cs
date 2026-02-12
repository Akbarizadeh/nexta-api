using Microsoft.AspNetCore.Mvc;
using Nexa.Api.DTOs;
using Nexa.Api.Services;

namespace Nexa.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;

    public AiController(IAiService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost("listing-from-image")]
    public async Task<ActionResult<AiListingFromImageResponse>> ListingFromImage(AiListingFromImageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ImageBase64))
            return BadRequest("Image data is required");

        var result = await _aiService.AnalyzeImageForListing(request.ImageBase64);
        return Ok(result);
    }

    [HttpPost("recommend")]
    public async Task<ActionResult<AiRecommendResponse>> Recommend(AiRecommendRequest request)
    {
        var result = await _aiService.GetRecommendations(request);
        return Ok(result);
    }

    [HttpPost("search")]
    public async Task<ActionResult<AiSearchResponse>> Search(AiSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest("Search query is required");

        var result = await _aiService.SmartSearch(request);
        return Ok(result);
    }
}
