using Microsoft.AspNetCore.Mvc;
using RiskListScraperAPI.Models;
using RiskListScraperAPI.Services;

namespace RiskListScraperAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RiskListController : ControllerBase
{
    private readonly RiskListSearchService _searchService;
    private readonly ILogger<RiskListController> _logger;

    public RiskListController(
        RiskListSearchService searchService,
        ILogger<RiskListController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Search for an entity across risk list databases
    /// </summary>
    /// <param name="request">Search request containing entity name and optional sources</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results from all queried sources</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(ApiResponse<SearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Search(
        [FromBody] SearchRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid request",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        try
        {
            var result = await _searchService.SearchAllSourcesAsync(
                request.EntityName,
                request.Sources,
                cancellationToken);

            return Ok(new ApiResponse<SearchResponse>
            {
                Success = true,
                Message = $"Search completed successfully. Found {result.TotalHits} total hits.",
                Data = result
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, new ApiResponse<object>
            {
                Success = false,
                Message = "Request was cancelled",
                Errors = new List<string> { "The operation was cancelled by the client" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing search request for entity: {EntityName}", request.EntityName);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get list of available sources
    /// </summary>
    /// <returns>List of available risk list sources</returns>
    [HttpGet("sources")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public IActionResult GetSources()
    {
        var sources = new List<string>
        {
            "OFAC",
            "World Bank",
            "Offshore Leaks Database"
        };

        return Ok(new ApiResponse<List<string>>
        {
            Success = true,
            Message = "Available sources retrieved successfully",
            Data = sources
        });
    }

    /// <summary>
    /// Search in OFAC (Office of Foreign Assets Control) database
    /// </summary>
    /// <param name="q">Entity name to search</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results from OFAC</returns>
    [HttpGet("ofac")]
    [ProducesResponseType(typeof(ApiResponse<SourceResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchOfac(
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid query",
                Errors = new List<string> { "Query parameter 'q' must be at least 2 characters long" }
            });
        }

        try
        {
            var result = await _searchService.SearchSourceAsync("OFAC", q, cancellationToken);

            return Ok(new ApiResponse<SourceResult>
            {
                Success = true,
                Message = $"OFAC search completed. Found {result.HitCount} results.",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching OFAC for entity: {EntityName}", q);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while searching OFAC",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Search in World Bank debarred firms database
    /// </summary>
    /// <param name="q">Entity name to search</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results from World Bank</returns>
    [HttpGet("worldbank")]
    [ProducesResponseType(typeof(ApiResponse<SourceResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchWorldBank(
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid query",
                Errors = new List<string> { "Query parameter 'q' must be at least 2 characters long" }
            });
        }

        try
        {
            var result = await _searchService.SearchSourceAsync("World Bank", q, cancellationToken);

            return Ok(new ApiResponse<SourceResult>
            {
                Success = true,
                Message = $"World Bank search completed. Found {result.HitCount} results.",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching World Bank for entity: {EntityName}", q);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while searching World Bank",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Search in ICIJ Offshore Leaks database
    /// </summary>
    /// <param name="q">Entity name to search</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results from Offshore Leaks</returns>
    [HttpGet("offshoreleaks")]
    [ProducesResponseType(typeof(ApiResponse<SourceResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchOffshoreLeaks(
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid query",
                Errors = new List<string> { "Query parameter 'q' must be at least 2 characters long" }
            });
        }

        try
        {
            var result = await _searchService.SearchSourceAsync("Offshore Leaks Database", q, cancellationToken);

            return Ok(new ApiResponse<SourceResult>
            {
                Success = true,
                Message = $"Offshore Leaks search completed. Found {result.HitCount} results.",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Offshore Leaks for entity: {EntityName}", q);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while searching Offshore Leaks",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>API health status</returns>
    [HttpGet("/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}
