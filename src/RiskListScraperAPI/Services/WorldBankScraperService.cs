using HtmlAgilityPack;
using RiskListScraperAPI.Models;
using System.Web;

namespace RiskListScraperAPI.Services;

public class WorldBankScraperService : IScraperService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WorldBankScraperService> _logger;
    private const string BASE_URL = "https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms";

    public WorldBankScraperService(IHttpClientFactory httpClientFactory, ILogger<WorldBankScraperService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _logger = logger;
    }

    public string GetSourceName() => "World Bank";

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
            _logger.LogInformation("Searching World Bank for entity: {EntityName}", entityName);

            // World Bank search
            var searchUrl = $"{BASE_URL}?firm={HttpUtility.UrlEncode(entityName)}";

            var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // Parse results - World Bank uses a specific table structure
            var resultRows = htmlDoc.DocumentNode.SelectNodes("//table[contains(@class, 'debarred')]//tr[position()>1]");

            if (resultRows != null && resultRows.Count > 0)
            {
                foreach (var row in resultRows)
                {
                    var cells = row.SelectNodes(".//td");
                    if (cells != null && cells.Count >= 5)
                    {
                        var record = new EntityRecord
                        {
                            Attributes = new Dictionary<string, string>
                            {
                                { "Firm Name", CleanText(cells[0].InnerText) },
                                { "Address", CleanText(cells[1].InnerText) },
                                { "Country", CleanText(cells[2].InnerText) },
                                { "From Date (Ineligibility Period)", CleanText(cells[3].InnerText) },
                                { "To Date (Ineligibility Period)", CleanText(cells[4].InnerText) },
                                { "Grounds", cells.Count > 5 ? CleanText(cells[5].InnerText) : "" }
                            }
                        };

                        result.Records.Add(record);
                    }
                }

                result.HitCount = result.Records.Count;
            }

            _logger.LogInformation("World Bank search completed. Found {Count} results", result.HitCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching World Bank for entity: {EntityName}", entityName);
            // Don't throw - return empty result to allow other sources to continue
        }

        return result;
    }

    private string CleanText(string text)
    {
        return HttpUtility.HtmlDecode(text?.Trim() ?? string.Empty);
    }
}
