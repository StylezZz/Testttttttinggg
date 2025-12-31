using RiskListScraperAPI.Models;

namespace RiskListScraperAPI.Services;

public interface IScraperService
{
    Task<SourceResult> SearchAsync(string entityName, CancellationToken cancellationToken = default);
    string GetSourceName();
}
