using HtmlAgilityPack;
using RiskListScraperAPI.Models;
using System.Web;

namespace RiskListScraperAPI.Services;

public class OfacScraperService : IScraperService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OfacScraperService> _logger;
    private const string BASE_URL = "https://sanctionssearch.ofac.treas.gov/";

    public OfacScraperService(IHttpClientFactory httpClientFactory, ILogger<OfacScraperService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _logger = logger;
    }

    public string GetSourceName() => "OFAC";

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
            _logger.LogInformation("Searching OFAC for entity: {EntityName}", entityName);

            // OFAC search URL construction
            var searchUrl = $"{BASE_URL}?name={HttpUtility.UrlEncode(entityName)}";

            var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // Parse results - OFAC uses a table structure for results
            var resultRows = htmlDoc.DocumentNode.SelectNodes("//table[@class='table']//tr[position()>1]");

            if (resultRows != null && resultRows.Count > 0)
            {
                foreach (var row in resultRows)
                {
                    var cells = row.SelectNodes(".//td");
                    if (cells != null && cells.Count >= 6)
                    {
                        var record = new EntityRecord
                        {
                            Attributes = new Dictionary<string, string>
                            {
                                { "Name", CleanText(cells[0].InnerText) },
                                { "Address", CleanText(cells[1].InnerText) },
                                { "Type", CleanText(cells[2].InnerText) },
                                { "Programs", CleanText(cells[3].InnerText) },
                                { "List", CleanText(cells[4].InnerText) },
                                { "Score", CleanText(cells[5].InnerText) }
                            }
                        };

                        result.Records.Add(record);
                    }
                }

                result.HitCount = result.Records.Count;
            }

            _logger.LogInformation("OFAC search completed. Found {Count} results", result.HitCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching OFAC for entity: {EntityName}", entityName);
            // Don't throw - return empty result to allow other sources to continue
        }

        return result;
    }

    private string CleanText(string text)
    {
        return HttpUtility.HtmlDecode(text?.Trim() ?? string.Empty);
    }
}
