using Microsoft.Playwright;
using RiskListScraperAPI.Models;
using System.Web;

namespace RiskListScraperAPI.Services;

public class OffshoreLeaksScraperService : IScraperService
{
    private readonly ILogger<OffshoreLeaksScraperService> _logger;
    private const string BASE_URL = "https://offshoreleaks.icij.org";

    public OffshoreLeaksScraperService(ILogger<OffshoreLeaksScraperService> logger)
    {
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

        IPlaywright? playwright = null;
        IBrowser? browser = null;

        try
        {
            _logger.LogInformation("Searching Offshore Leaks for entity: {EntityName} using Playwright", entityName);

            // Initialize Playwright
            playwright = await Playwright.CreateAsync();

            // Launch browser in headless mode
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            // Create a new page
            var page = await browser.NewPageAsync();

            // Navigate to search URL
            var searchUrl = $"{BASE_URL}/search?q={HttpUtility.UrlEncode(entityName)}";
            await page.GotoAsync(searchUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000 // 60 seconds timeout
            });

            // Wait for search results to load - the page loads dynamically with JavaScript
            // Wait for the results container to be visible
            try
            {
                await page.WaitForSelectorAsync(".search-result, .result-entity, [class*='result']", new PageWaitForSelectorOptions
                {
                    Timeout = 30000 // 30 seconds
                });
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("No results found or timeout waiting for results for entity: {EntityName}", entityName);
                return result;
            }

            // Extract results from the DOM
            // ICIJ Offshore Leaks typically shows results in a list/card format
            var resultElements = await page.QuerySelectorAllAsync(".search-result, .result-entity, [class*='SearchResult']");

            if (resultElements.Any())
            {
                foreach (var element in resultElements)
                {
                    try
                    {
                        // Extract Entity name
                        var entityElement = await element.QuerySelectorAsync("h3, h4, .entity-name, [class*='entityName'], [class*='EntityName']");
                        var entity = entityElement != null ? await entityElement.InnerTextAsync() : "";

                        // Extract Jurisdiction
                        var jurisdictionElement = await element.QuerySelectorAsync("[class*='jurisdiction'], dt:has-text('Jurisdiction') + dd, .country");
                        var jurisdiction = jurisdictionElement != null ? await jurisdictionElement.InnerTextAsync() : "";

                        // Extract Linked To
                        var linkedToElement = await element.QuerySelectorAsync("[class*='linked'], dt:has-text('Linked') + dd, [class*='connection']");
                        var linkedTo = linkedToElement != null ? await linkedToElement.InnerTextAsync() : "";

                        // Extract Data From
                        var dataFromElement = await element.QuerySelectorAsync("[class*='source'], dt:has-text('Data') + dd, [class*='dataset']");
                        var dataFrom = dataFromElement != null ? await dataFromElement.InnerTextAsync() : "";

                        var record = new EntityRecord
                        {
                            Attributes = new Dictionary<string, string>
                            {
                                { "Entity", CleanText(entity) },
                                { "Jurisdiction", CleanText(jurisdiction) },
                                { "Linked To", CleanText(linkedTo) },
                                { "Data From", CleanText(dataFrom) }
                            }
                        };

                        result.Records.Add(record);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing individual result element");
                        continue;
                    }
                }

                result.HitCount = result.Records.Count;
            }

            _logger.LogInformation("Offshore Leaks search completed using Playwright. Found {Count} results", result.HitCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Offshore Leaks for entity: {EntityName}", entityName);
        }
        finally
        {
            // Cleanup
            if (browser != null)
            {
                await browser.CloseAsync();
            }
            playwright?.Dispose();
        }

        return result;
    }

    private string CleanText(string? text)
    {
        return HttpUtility.HtmlDecode(text?.Trim() ?? string.Empty);
    }
}
