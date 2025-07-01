using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging; // ✅ Make sure this using is present
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http; // ✅ Add this using for HttpRequestException

public class GameSearchModel : PageModel
{
    private readonly IGameService _gameService;
    private readonly ILogger<GameSearchModel> _logger; 

    public GameSearchModel(IGameService gameService, ILogger<GameSearchModel> logger)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "IGameService cannot be null.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger cannot be null.");
    }

    public async Task<IActionResult> OnGetSearchAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            _logger.LogInformation("OnGetSearchAsync: Search term was empty or whitespace. Returning empty results."); 
            return new JsonResult(new { success = true, data = new List<GamePreviewDTO>() });
        }

        try
        {
            _logger.LogInformation("OnGetSearchAsync: Starting search for term: {SearchTerm}", term); 

            var results = await _gameService.SearchGamePreviewsByNameAsync(term);
            results ??= new List<GamePreviewDTO>(); 

            _logger.LogInformation("OnGetSearchAsync: Search for '{SearchTerm}' completed. Found {ResultCount} results.", term, results.Count); // ✅ Log successful search

            return new JsonResult(new { success = true, data = results });
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnGetSearchAsync: HttpRequestException during game search for term: {SearchTerm}. Message: {ErrorMessage}", term, ex.Message); // ✅ Log the specific network error
            return new JsonResult(new { success = false, message = "There was a connection issue when performing the search. Please try again." })
            {
                StatusCode = 503 
            };
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "OnGetSearchAsync: InvalidOperationException during game search for term: {SearchTerm}. Message: {ErrorMessage}", term, ex.Message); // ✅ Log the specific operational error
            return new JsonResult(new { success = false, message = "An operational error occurred during the search. Please contact support." })
            {
                StatusCode = 500 
            };
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnGetSearchAsync: An unexpected error occurred during game search for term: {SearchTerm}. Message: {ErrorMessage}", term, ex.Message); // ✅ Log the general unexpected error
            return new JsonResult(new { success = false, message = "An unexpected error occurred during the search." })
            {
                StatusCode = 500 
            };
        }
    }
}