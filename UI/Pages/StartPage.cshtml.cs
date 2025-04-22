using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UI.Pages;

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class StartPageModel : PageModel
{
    private readonly ILogger<StartPageModel> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public StartPageModel(ILogger<StartPageModel> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public List<Juego> Juegos { get; set; }

    public async Task OnGetAsync()
    {
        /*
        string apiUrl = "https://jsonplaceholder.typicode.com/todos/1"; // Reemplaza con la URL real

        var httpClient = _httpClientFactory.CreateClient();

        try
        {
            var response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var juegosDesdeApi = JsonSerializer.Deserialize<List<Juego>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (juegosDesdeApi != null)
                {
                    Juegos = juegosDesdeApi.Take(6).ToList();
                }
                else
                {
                    Juegos = new List<Juego>();
                    _logger.LogWarning("La respuesta de la API de juegos fue vacía o no se pudo deserializar.");
                }
            }
            else
            {
                Juegos = new List<Juego>();
                _logger.LogError($"Error al llamar a la API de juegos. Status Code: {response.StatusCode}");
                // Puedes agregar más detalles del error si la API los proporciona
            }
        }
        catch (HttpRequestException ex)
        {
            Juegos = new List<Juego>();
            _logger.LogError($"Error de conexión al servicio de juegos: {ex.Message}");
        } */
    }
}

// Define la clase Juego (asegúrate de que coincida con la estructura de tu API)
public class Juego
{
    public string Titulo { get; set; }
    public string ImagenUrl { get; set; }
    public string DescripcionCorta { get; set; }
    // ... otras propiedades que devuelva tu API
}