using Nexa.Api.DTOs;
namespace Nexa.Api.Services;
public interface IAiService
{
    Task<AiListingFromImageResponse> AnalyzeImageForListing(string imageBase64);
    Task<AiRecommendResponse> GetRecommendations(AiRecommendRequest request);
    Task<AiSearchResponse> SmartSearch(AiSearchRequest request);
}
