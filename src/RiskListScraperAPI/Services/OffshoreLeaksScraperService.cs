using HtmlAgilityPack;
using RiskListScraperAPI.Models;
using System.Web;

namespace RiskListScraperAPI.Services;

public class OffshoreLeaksScraperService : IScraperService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OffshoreLeaksScraperService> _logger;
    private const string BASE_URL = "https://offshoreleaks.icij.org";

    public OffshoreLeaksScraperService(IHttpClientFactory httpClientFactory, ILogger<OffshoreLeaksScraperService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _logger = logger;
    }

    public string GetSourceName() => "Offshore Leaks Database";

    public async Task<SourceResult> SearchAsync(string entityName, CancellationToken cancellationToken = default)
    {
        var result = new SourceResult
        {
            Source = GetSourceName(),
            HitCount = 0,
            Records = new List<EntityRecord>()
        };

        try
        {
            _logger.LogInformation("Searching Offshore Leaks for entity: {EntityName}", entityName);

            // Offshore Leaks search
            var searchUrl = $"{BASE_URL}/search?q={HttpUtility.UrlEncode(entityName)}";

            var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // Parse results - Offshore Leaks uses a card/list structure
            var resultNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'result-item')]");

            if (resultNodes != null && resultNodes.Count > 0)
            {
                foreach (var node in resultNodes)
                {
                    var record = new EntityRecord
                    {
                        Attributes = new Dictionary<string, string>
                        {
                            { "Entity", CleanText(node.SelectSingleNode(".//h3 | .//h4")?.InnerText) },
                            { "Jurisdiction", CleanText(node.SelectSingleNode(".//*[contains(text(), 'Jurisdiction')]/..//span")?.InnerText) },
                            { "Linked To", CleanText(node.SelectSingleNode(".//*[contains(text(), 'Linked')]/..//span")?.InnerText) },
                            { "Data From", CleanText(node.SelectSingleNode(".//*[contains(text(), 'Data')]/..//span")?.InnerText) }
                        }
                    };

                    result.Records.Add(record);
                }

                result.HitCount = result.Records.Count;
            }

            _logger.LogInformation("Offshore Leaks search completed. Found {Count} results", result.HitCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Offshore Leaks for entity: {EntityName}", entityName);
            // Don't throw - return empty result to allow other sources to continue
        }

        return result;
    }

    private string CleanText(string? text)
    {
        return HttpUtility.HtmlDecode(text?.Trim() ?? string.Empty);
    }
}
