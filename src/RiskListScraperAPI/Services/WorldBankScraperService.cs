using RiskListScraperAPI.Models;
using System.Text.Json;
using System.Web;

namespace RiskListScraperAPI.Services;

public class WorldBankScraperService : IScraperService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WorldBankScraperService> _logger;
    private const string API_URL = "https://apigwext.worldbank.org/dvsvc/v1.0/json/APPLICATION/ADOBE_EXPRNCE_MGR/FIRM/SANCTIONED_FIRM";
    private const string API_KEY = "z9duUaFUiEUYSHs97CU38fcZO7ipOPvm";

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
            _logger.LogInformation("Searching World Bank API for entity: {EntityName}", entityName);

            // Create request with API key header
            var request = new HttpRequestMessage(HttpMethod.Get, API_URL);
            request.Headers.Add("apikey", API_KEY);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);

            // Parse JSON response
            using var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            // The API might return data in different structures
            // Try to find the array of sanctioned firms
            JsonElement firmsArray;

            if (root.TryGetProperty("response", out var responseObj) &&
                responseObj.TryGetProperty("ZPROCSUPP", out firmsArray) ||
                root.TryGetProperty("ZPROCSUPP", out firmsArray) ||
                root.ValueKind == JsonValueKind.Array)
            {
                if (root.ValueKind == JsonValueKind.Array)
                {
                    firmsArray = root;
                }

                foreach (var firm in firmsArray.EnumerateArray())
                {
                    try
                    {
                        // Extract firm details
                        var firmName = GetJsonProperty(firm, "FIRM_NAME", "firmName", "name");

                        // Filter by entity name if it matches (case-insensitive partial match)
                        if (string.IsNullOrEmpty(entityName) ||
                            firmName.Contains(entityName, StringComparison.OrdinalIgnoreCase))
                        {
                            var record = new EntityRecord
                            {
                                Attributes = new Dictionary<string, string>
                                {
                                    { "Firm Name", firmName },
                                    { "Address", GetJsonProperty(firm, "ADDRESS", "address", "ADDR") },
                                    { "Country", GetJsonProperty(firm, "COUNTRY", "country", "LAND1") },
                                    { "Ineligibility From Date", GetJsonProperty(firm, "DEBAR_FROM_DATE", "fromDate", "ineligibilityFromDate") },
                                    { "Ineligibility To Date", GetJsonProperty(firm, "DEBAR_TO_DATE", "toDate", "ineligibilityToDate") },
                                    { "Grounds", GetJsonProperty(firm, "GROUNDS", "grounds", "reason") },
                                    { "Sanction Type", GetJsonProperty(firm, "SANCTION_TYPE", "sanctionType") }
                                }
                            };

                            result.Records.Add(record);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing individual firm record");
                        continue;
                    }
                }

                result.HitCount = result.Records.Count;
            }

            _logger.LogInformation("World Bank API search completed. Found {Count} results", result.HitCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching World Bank API for entity: {EntityName}", entityName);
            // Don't throw - return empty result to allow other sources to continue
        }

        return result;
    }

    private string GetJsonProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                var value = property.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }
        }
        return string.Empty;
    }
}
