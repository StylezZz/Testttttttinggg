namespace RiskListScraperAPI.Models;

public class SearchResponse
{
    public string EntityName { get; set; } = string.Empty;
    public int TotalHits { get; set; }
    public List<SourceResult> Results { get; set; } = new();
    public DateTime SearchTimestamp { get; set; } = DateTime.UtcNow;
}

public class SourceResult
{
    public string Source { get; set; } = string.Empty;
    public int HitCount { get; set; }
    public List<EntityRecord> Records { get; set; } = new();
}

public class EntityRecord
{
    public Dictionary<string, string> Attributes { get; set; } = new();
}
