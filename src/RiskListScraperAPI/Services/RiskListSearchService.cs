using RiskListScraperAPI.Models;

namespace RiskListScraperAPI.Services;

public class RiskListSearchService
{
    private readonly IEnumerable<IScraperService> _scraperServices;
    private readonly ILogger<RiskListSearchService> _logger;

    public RiskListSearchService(
        IEnumerable<IScraperService> scraperServices,
        ILogger<RiskListSearchService> logger)
    {
        _scraperServices = scraperServices;
        _logger = logger;
    }

    public async Task<SearchResponse> SearchAllSourcesAsync(
        string entityName,
        List<string>? specificSources = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting search for entity: {EntityName}", entityName);

        var response = new SearchResponse
        {
            EntityName = entityName,
            SearchTimestamp = DateTime.UtcNow
        };

        // Filter scrapers if specific sources requested
        var scrapersToUse = _scraperServices;
        if (specificSources != null && specificSources.Any())
        {
            scrapersToUse = _scraperServices.Where(s =>
                specificSources.Contains(s.GetSourceName(), StringComparer.OrdinalIgnoreCase));
        }

        // Search all sources in parallel
        var searchTasks = scrapersToUse.Select(scraper =>
            scraper.SearchAsync(entityName, cancellationToken));

        var results = await Task.WhenAll(searchTasks);

        response.Results = results.ToList();
        response.TotalHits = results.Sum(r => r.HitCount);

        _logger.LogInformation(
            "Search completed for entity: {EntityName}. Total hits: {TotalHits}",
            entityName,
            response.TotalHits);

        return response;
    }
}
