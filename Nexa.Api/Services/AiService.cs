using Microsoft.EntityFrameworkCore;
using Nexa.Api.Data;
using Nexa.Api.DTOs;
using Nexa.Api.Models;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

namespace Nexa.Api.Services;

public class AiService : IAiService
{
    private readonly ILogger<AiService> _logger;
    private readonly IConfiguration _configuration;
    private readonly OpenAIClient _openAiClient;
    private readonly NexaDbContext _db;


    public AiService(ILogger<AiService> logger, IConfiguration configuration , NexaDbContext db)
    {
        _logger = logger;
        _configuration = configuration;
        var apiKey = _configuration["OPENAI_API_KEY"];
        //var apiKey = "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";// _configuration["OpenAI:ApiKey"];
        _openAiClient = new OpenAIClient(apiKey);
        _db = db;
    }

    public async Task<AiListingFromImageResponse> AnalyzeImageForListing(string imageBase64)
    {
        _logger.LogInformation("Analyzing image for listing creation with OpenAI Vision");

        try
        {
            var chatClient = _openAiClient.GetChatClient("gpt-4o");

            var messages = new List<ChatMessage>
        {
            new SystemChatMessage(@"You are an AI assistant for a second-hand marketplace app similar to Divar.

The user uploads an image of an item they want to sell.

Your task:
1. Analyze the image carefully.
2. Detect the main object being sold.
3. Generate a single realistic listing.

Rules:
- This can be any object: car, phone, furniture, food, tools, decoration, etc.
- Assume the seller is a normal person.
- Write in natural, human language.
- Do NOT invent technical specs you cannot see.
- Category must be one of:
  Vehicles, Electronics, Home, Furniture, Tools, Fashion, Services, Other

Return ONLY valid JSON in exactly this structure:

{
  ""title"": """",
  ""description"": """",
  ""category"": """",
  ""tags"": [],
  ""priceMin"": 0,
  ""priceMax"": 0,
  ""confidenceScore"": 0.0
}

Guidelines for fields:
- title: short and catchy marketplace title
- description: 2–4 natural sentences, like real user listings
- tags: 5–10 relevant keywords
- priceMin/priceMax: rough estimated range (if unknown use 0)
- confidenceScore: how confident you are about detection (0.7–1.0)"),
            new UserChatMessage(
                ChatMessageContentPart.CreateTextPart("Analyze this product image:"),
                ChatMessageContentPart.CreateImagePart(
                    BinaryData.FromBytes(Convert.FromBase64String(imageBase64)),
                    "image/jpeg"
                )
            )
        };

            var response = await chatClient.CompleteChatAsync(messages);
            var raw = response.Value.Content[0].Text;

            // مرحله حیاتی: پاک‌سازی JSON
            var jsonResponse = ExtractJson(raw);

            AiListingFromImageResponse aiResult;
            try
            {
                aiResult = JsonSerializer.Deserialize<AiListingFromImageResponse>(
                    jsonResponse,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                return aiResult;
            }
            catch (Exception ex)
            {
                throw new Exception("AI returned invalid JSON", ex);
            }

            //if (aiResult == null)
            //    throw new Exception("AI result is null");

            //if (aiResult.ListingDrafts == null || !aiResult.ListingDrafts.Any())
            //    throw new Exception("AI returned no listing drafts");

            //// بهترین آگهی
            //var bestListing = aiResult.ListingDrafts
            //    .OrderByDescending(x => x.ConfidenceScore)
            //    .First();

            //return new AiListingFromImageResponse(
            //    Title: bestListing.Title,
            //    Description: bestListing.Description,
            //    Category: bestListing.Category,
            //    Tags: aiResult.Keywords,
            //    PriceMin: aiResult.PriceRange.Min,
            //    PriceMax: aiResult.PriceRange.Max,
            //    ConfidenceScore: bestListing.ConfidenceScore,
            //    InterpretedIntent:""
            //);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image with OpenAI");

            return new AiListingFromImageResponse(
                Title: "Product",
                Description: "Unable to analyze image. Please add details manually.",
                Category: "Other",
                Tags: new List<string> { "product" },
                PriceMin: 10.00m,
                PriceMax: 100.00m,
                ConfidenceScore: 0.0,
                InterpretedIntent: ""
            );
        }
    }
    string ExtractJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new Exception("AI response is empty");

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');

        if (start == -1 || end == -1 || end <= start)
            throw new Exception("No valid JSON found in AI response");

        return text.Substring(start, end - start + 1);
    }
    public async Task<AiRecommendResponse> GetRecommendations(AiRecommendRequest request)
    {
        _logger.LogInformation("Generating recommendations for user {UserId}", request.UserId);

        try
        {
            var chatClient = _openAiClient.GetChatClient("gpt-4o");

            var prompt = $@"Generate personalized recommendations for a user based on:  
                - Location: {request.Latitude}, {request.Longitude}  
                - Interests: {string.Join(", ", request.Interests ?? new List<string>())}  
                - Time context: {request.TimeContext ?? "general"}  
                  
                Return a JSON array of 3-5 recommended items with:  
                - contentType: 'Listing' or 'Event'  
                - title: item title  
                - description: brief description  
                - category: relevant category  
                - relevanceScore: 0.0-1.0  
                  
                Return ONLY valid JSON array, no other text.";

            var response = await chatClient.CompleteChatAsync(new SystemChatMessage(prompt));
            var jsonResponse = response.Value.Content[0].Text;

            var results = JsonSerializer.Deserialize<List<AiRecommendationResult>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var items = results.Select(r => new RecommendedItem(
                ContentType: r.contentType,
                ContentId: Guid.NewGuid(),
                Title: r.title,
                Description: r.description,
                ImageUrl: null,
                Category: r.category,
                RelevanceScore: r.relevanceScore,
                DistanceKm: 0.0
            )).ToList();

            return new AiRecommendResponse(Items: items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations with OpenAI");

            return new AiRecommendResponse(
                Items: new List<RecommendedItem>
                {
                    new(
                        ContentType: "Listing",
                        ContentId: Guid.NewGuid(),
                        Title: "Sample Recommended Product",
                        Description: "A product matched to your interests and location.",
                        ImageUrl: null,
                        Category: "Electronics",
                        RelevanceScore: 0.92,
                        DistanceKm: 1.5
                    )
                }
            );
        }
    }

    public async Task<AiSearchResponse> SmartSearch(AiSearchRequest request)
    {
        _logger.LogInformation("Processing fuzzy search: {Query}", request.Query);

        var results = new List<RecommendedItem>();
        var searchTerms = request.Query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // جستجو در Listings  
        var listings = await _db.Listings
            .Include(l => l.Seller)
            .Include(l => l.Business)
            .Where(l => l.Status == Models.ListingStatus.Active)
            .ToListAsync();

        foreach (var listing in listings)
        {
            var score = CalculateFuzzyScore(searchTerms, listing.Title, listing.Description, listing.Category, listing.Tags);

            if (score > 0.3) // حداقل 30% تطابق  
            {
                var distance = 0.0;
                if (request.Latitude != 0 && request.Longitude != 0 &&
                    listing.Latitude.HasValue && listing.Longitude.HasValue)
                {
                    distance = CalculateDistance(
                        request.Latitude, request.Longitude,
                        listing.Latitude.Value, listing.Longitude.Value);
                }

                results.Add(new RecommendedItem(
                    ContentType: "Listing",
                    ContentId: listing.Id,
                    Title: listing.Title,
                    Description: listing.Description,
                    ImageUrl: listing.ImageUrls.FirstOrDefault(),
                    Category: listing.Category,
                    RelevanceScore: score,
                    DistanceKm: distance
                ));
            }
        }

        // جستجو در Events  
        var events = await _db.Events
            .Include(e => e.Business)
            .Where(e => e.EndDate == null || e.EndDate > DateTime.UtcNow)
            .ToListAsync();

        foreach (var evt in events)
        {
            var score = CalculateFuzzyScore(searchTerms, evt.Title, evt.Description, evt.Category, evt.Tags);

            if (score > 0.3)
            {
                var distance = 0.0;
                if (request.Latitude != 0 && request.Longitude != 0 &&
                    evt.Latitude.HasValue && evt.Longitude.HasValue)
                {
                    distance = CalculateDistance(
                        request.Latitude, request.Longitude,
                        evt.Latitude.Value, evt.Longitude.Value);
                }

                results.Add(new RecommendedItem(
                    ContentType: "Event",
                    ContentId: evt.Id,
                    Title: evt.Title,
                    Description: evt.Description,
                    ImageUrl: evt.ImageUrl,
                    Category: evt.Category,
                    RelevanceScore: score,
                    DistanceKm: distance
                ));
            }
        }

        // جستجو در Offers  
        var offers = await _db.Offers
            .Include(o => o.Business)
            .Where(o => o.EndDate > DateTime.UtcNow)
            .ToListAsync();

        foreach (var offer in offers)
        {
            var score = CalculateFuzzyScore(searchTerms, offer.Title, offer.Description, offer.Category, offer.Tags);

            if (score > 0.3)
            {
                var distance = 0.0;
                if (request.Latitude != 0 && request.Longitude != 0 &&
                    offer.Latitude.HasValue && offer.Longitude.HasValue)
                {
                    distance = CalculateDistance(
                        request.Latitude, request.Longitude,
                        offer.Latitude.Value, offer.Longitude.Value);
                }

                results.Add(new RecommendedItem(
                    ContentType: "Offer",
                    ContentId: offer.Id,
                    Title: offer.Title,
                    Description: offer.Description,
                    ImageUrl: offer.ImageUrl,
                    Category: offer.Category,
                    RelevanceScore: score,
                    DistanceKm: distance
                ));
            }
        }

        // مرتب‌سازی بر اساس relevance score  
        var sortedResults = results
            .OrderByDescending(r => r.RelevanceScore)
            .ThenBy(r => r.DistanceKm)
            .Take(20)
            .ToList();

        return new AiSearchResponse(
            InterpretedIntent: $"جستجو برای: {request.Query}",
            Results: sortedResults
        );
    }

    private double CalculateFuzzyScore(string[] searchTerms, string title, string? description, string category, List<string> tags)
    {
        var score = 0.0;
        var titleLower = title.ToLower();
        var descLower = description?.ToLower() ?? "";
        var categoryLower = category.ToLower();
        var tagsLower = tags.Select(t => t.ToLower()).ToList();

        foreach (var term in searchTerms)
        {
            // تطابق کامل در عنوان (امتیاز بالا)  
            if (titleLower.Contains(term))
                score += 1.0;

            // تطابق کامل در توضیحات  
            if (descLower.Contains(term))
                score += 0.5;

            // تطابق در دسته‌بندی  
            if (categoryLower.Contains(term))
                score += 0.7;

            // تطابق در تگ‌ها  
            if (tagsLower.Any(tag => tag.Contains(term)))
                score += 0.6;

            // تطابق فازی (شباهت رشته‌ای)  
            if (titleLower.Length > 0 && CalculateLevenshteinSimilarity(term, titleLower) > 0.7)
                score += 0.4;
        }

        // نرمال‌سازی امتیاز  
        return Math.Min(score / searchTerms.Length, 1.0);
    }

    private double CalculateLevenshteinSimilarity(string s1, string s2)
    {
        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);
        return maxLength == 0 ? 1.0 : 1.0 - ((double)distance / maxLength);
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost
                );
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371; // شعاع زمین به کیلومتر  
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    // Helper class for AI analysis result  
    private class AiSearchParams
    {
        public string intent { get; set; }
        public List<string> keywords { get; set; }
        public string category { get; set; }
        public decimal? priceMin { get; set; }
        public decimal? priceMax { get; set; }
    }


    private class AiRecommendationResult
    {
        public string contentType { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string category { get; set; }
        public double relevanceScore { get; set; }
    }

    private class AiSearchResult
    {
        public string intent { get; set; }
        public List<AiRecommendationResult> results { get; set; }
    }
}