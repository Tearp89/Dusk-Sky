using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

public class ListCreatorModel : PageModel
{
    private readonly IGameService _gameService;
    private readonly IGameListService _listService;
    private readonly IGameListItemService _itemService;

    public ListCreatorModel(IGameService gameService, IGameListService listService, IGameListItemService itemService)
    {
        _gameService = gameService;
        _listService = listService;
        _itemService = itemService;
    }

    [BindProperty] public string? DefaultName { get; set; }
    [BindProperty] public string? GameId { get; set; }
    [BindProperty] public string ListName { get; set; } = string.Empty;
    [BindProperty] public string Tags { get; set; } = string.Empty;
    [BindProperty] public string Visibility { get; set; } = "private";
    [BindProperty] public bool Ranked { get; set; } = false;
    [BindProperty] public string Description { get; set; } = string.Empty;
    [BindProperty] public string? GameSearchQuery { get; set; }
    public List<GamePreviewDTO> SearchResults { get; set; } = new();
    public List<GamePreviewDTO> SelectedGames { get; set; } = new();


    public GamePreviewDTO? GamePreview { get; set; }

    private const string SessionKey = "SelectedGameIds";

    public async Task<IActionResult> OnGetAsync(bool clear = false)
    {
        if (clear)
        {
            ListName = string.Empty;
            Description = string.Empty;
            Visibility = "private";
            HttpContext.Session.Remove(SessionKey);
        }
        else
        {
            var ids = HttpContext.Session.GetString(SessionKey);
            if (!string.IsNullOrEmpty(ids))
            {
                var guidList = ids.Split(',').Select(Guid.Parse).ToList();
                foreach (var id in guidList)
                {
                    var preview = await _gameService.GetGamePreviewByIdAsync(id);
                    if (preview != null)
                        SelectedGames.Add(preview);
                }
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var newList = new GameListDTO
        {
            Id = Guid.NewGuid().ToString(),
            Name = ListName,
            Description = Description,
            IsPublic = Visibility == "public",
            UserId = userId
        };

        var listId = await _listService.CreateListAsync(newList);
        if (string.IsNullOrWhiteSpace(listId))
        {
            TempData["ErrorMessage"] = "No se pudo crear la lista. Inténtalo de nuevo.";
            return RedirectToPage();
        }

        var ids = HttpContext.Session.GetString(SessionKey);
        if (!string.IsNullOrEmpty(ids))
        {
            var gameIds = ids.Split(',').Select(Guid.Parse).ToList();
            int order = 1;
            foreach (var gameId in gameIds)
            {
                var item = new GameListItemDTO
                {
                    Id = Guid.NewGuid().ToString(),
                    ListId = listId,
                    GameId = gameId,
                    Order = order++,
                    Notes = ""
                };

                await _itemService.AddItemAsync(item);
            }
        }

        HttpContext.Session.Remove(SessionKey);
        TempData["SuccessMessage"] = "¡Lista creada exitosamente!";
        return RedirectToPage(new { clear = true });
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        if (!string.IsNullOrWhiteSpace(GameSearchQuery))
        {
            SearchResults = await _gameService.SearchGamePreviewsByNameAsync(GameSearchQuery);
        }

        TempData["ListName"] = ListName;
        TempData["Tags"] = Tags;
        TempData["Description"] = Description;
        TempData["Visibility"] = Visibility;
        TempData["Ranked"] = Ranked;


        await OnGetAsync(); // para recargar juegos seleccionados
        return Page();
    }

    public async Task<IActionResult> OnPostAddGameAsync(Guid gameId)
    {
        var ids = HttpContext.Session.GetString("SelectedGameIds");
        var list = string.IsNullOrEmpty(ids) ? new List<Guid>() : ids.Split(',').Select(Guid.Parse).ToList();

        if (!list.Contains(gameId))
            list.Add(gameId);

        HttpContext.Session.SetString("SelectedGameIds", string.Join(',', list));
        TempData["ListName"] = ListName;
        TempData["Tags"] = Tags;
        TempData["Description"] = Description;
        TempData["Visibility"] = Visibility;
        TempData["Ranked"] = Ranked;
        return RedirectToPage(); // recarga y mostrará el juego añadido
    }

}