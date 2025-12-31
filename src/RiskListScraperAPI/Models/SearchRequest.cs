using System.ComponentModel.DataAnnotations;

namespace RiskListScraperAPI.Models;

public class SearchRequest
{
    [Required(ErrorMessage = "Entity name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Entity name must be between 2 and 200 characters")]
    public string EntityName { get; set; } = string.Empty;

    public List<string>? Sources { get; set; } // Optional: specify which sources to search
}
