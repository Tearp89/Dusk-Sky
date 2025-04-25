/*using Microsoft.AspNetCore.Mvc;
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
        } 
    }
}

// Define la clase Juego (asegúrate de que coincida con la estructura de tu API)
public class Juego
{
    public string Titulo { get; set; }
    public string ImagenUrl { get; set; }
    public string DescripcionCorta { get; set; }
    // ... otras propiedades que devuelva tu API
}*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class Juego
{
    public string Titulo { get; set; }
    public string ImagenUrl { get; set; }
    public string DescripcionCorta { get; set; }
    // ... otras propiedades que devuelva tu API
}

public class Comentario
{
    public string Usuario { get; set; }
    public string Texto { get; set; }
    public DateTime Fecha { get; set; }
    public string AvatarUrl { get; set; } = "https://i.pravatar.cc/32";
    public int Likes { get; set; }
}


public class StartPageModel : PageModel
{
    private readonly ILogger<StartPageModel> _logger;
    public Dictionary<string, List<Juego>> SeccionesJuegos { get; set; }
    // private readonly IHttpClientFactory _httpClientFactory; // Comentado por ahora

    public StartPageModel(ILogger<StartPageModel> logger/*, IHttpClientFactory httpClientFactory*/) // Comentado por ahora
    {
        _logger = logger;
        // _httpClientFactory = httpClientFactory; // Comentado por ahora
        Juegos = new List<Juego>(); // Inicializa la lista para evitar NullReferenceException
        Comentarios = new List<Comentario>(); // Inicializa la lista de comentarios
    }

    public List<Juego> Juegos { get; set; }
    public List<Comentario> Comentarios { get; set; }

    public async Task OnGetAsync()
    {
        // Simulación de datos de juegos
        var juegosSimulados = new List<Juego>()
        {
            new Juego { Titulo = "The Last of Us Part II", ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain", DescripcionCorta = "Una intensa historia de venganza." },
            new Juego { Titulo = "Ghost of Tsushima", ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain", DescripcionCorta = "Un samurái en la isla de Tsushima." },
            new Juego { Titulo = "Marvel's Spider-Man: Miles Morales", ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain", DescripcionCorta = "La nueva aventura de Spider-Man." },
            new Juego { Titulo = "Horizon Forbidden West", ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain", DescripcionCorta = "Aloy viaja al oeste prohibido." },
            new Juego { Titulo = "Elden Ring", ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain", DescripcionCorta = "Un nuevo mundo de fantasía oscura." },
            new Juego { Titulo = "Cyberpunk 2077", ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain", DescripcionCorta = "Un RPG de mundo abierto en Night City." },
            new Juego { Titulo = "God of War Ragnarök", ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain", DescripcionCorta = "La continuación de la saga nórdica." },
            new Juego { Titulo = "Gran Turismo 7", ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain", DescripcionCorta = "La experiencia de conducción real." },
            new Juego { Titulo = "Returnal", ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain", DescripcionCorta = "Un shooter roguelike en un planeta hostil." },
            new Juego { Titulo = "Ratchet & Clank: Rift Apart", ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain", DescripcionCorta = "Una aventura interdimensional." },
            new Juego { Titulo = "Final Fantasy VII Remake Intergrade", ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain", DescripcionCorta = "Una reimaginación del clásico RPG." },
            new Juego { Titulo = "Stray", ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain", DescripcionCorta = "Un gato perdido en una ciudad cibernética." },
            // Puedes agregar más juegos si quieres verlos en las siguientes secciones
        };

        Juegos = juegosSimulados.Take(12).ToList(); // Tomamos los primeros 12 para las dos primeras secciones

        // Simulación de comentarios
        var comentariosSimulados = new List<Comentario>
{
    new Comentario { Usuario = "Usuario1", Texto = "¡Me encantó este juego!", Fecha = DateTime.Now.AddDays(-2), Likes = 134 },
    new Comentario { Usuario = "GamerPro", Texto = "Gráficos impresionantes y jugabilidad fluida.", Fecha = DateTime.Now.AddDays(-5), Likes = 87 },
    new Comentario { Usuario = "CriticoGamer", Texto = "Una experiencia inmersiva.", Fecha = DateTime.Now.AddDays(-1), Likes = 192 },
    new Comentario { Usuario = "JugadorCasual", Texto = "Muy divertido para pasar el rato.", Fecha = DateTime.Now.AddDays(-7), Likes = 65 },
    new Comentario { Usuario = "FanDeLaSaga", Texto = "¡El mejor de la serie!", Fecha = DateTime.Now.AddDays(-3), Likes = 220 },
    new Comentario { Usuario = "NuevoJugador", Texto = "Recién lo empiezo y ya me gusta.", Fecha = DateTime.Now.AddDays(-4), Likes = 47 },
    new Comentario { Usuario = "Experto4K", Texto = "Se ve increíble en mi TV 4K.", Fecha = DateTime.Now.AddDays(-6), Likes = 76 },
    new Comentario { Usuario = "Velocista", Texto = "La historia te atrapa desde el principio.", Fecha = DateTime.Now.AddDays(-8), Likes = 58 },
    new Comentario { Usuario = "Explorador", Texto = "El mundo es enorme y lleno de secretos.", Fecha = DateTime.Now.AddDays(-9), Likes = 82 },
    new Comentario { Usuario = "MultiplayerFan", Texto = "Las opciones multijugador son geniales.", Fecha = DateTime.Now.AddDays(-10), Likes = 105 },
};



        Comentarios = comentariosSimulados.OrderByDescending(c => c.Fecha).Take(10).ToList(); // Tomamos los 10 comentarios más recientes
        SeccionesJuegos = new Dictionary<string, List<Juego>>
        {
            ["Top Ventas"] = new List<Juego>
            {
                new Juego { Titulo = "Elden Ring", ImagenUrl = "https://th.bing.com/th/id/R.b1370f1b7368d8e876d64e159e5f1d56?rik=NTY44blNsUxi%2fA&pid=ImgRaw&r=0" },
                new Juego { Titulo = "God of War", ImagenUrl = "https://th.bing.com/th/id/R.b1370f1b7368d8e876d64e159e5f1d56?rik=NTY44blNsUxi%2fA&pid=ImgRaw&r=0" },
                new Juego { Titulo = "Zelda TOTK", ImagenUrl = "https://th.bing.com/th/id/R.b1370f1b7368d8e876d64e159e5f1d56?rik=NTY44blNsUxi%2fA&pid=ImgRaw&r=0" }
            },
            ["Nuevos Lanzamientos"] = new List<Juego>
            {
                new Juego { Titulo = "Starfield", ImagenUrl = "https://th.bing.com/th/id/R.b1370f1b7368d8e876d64e159e5f1d56?rik=NTY44blNsUxi%2fA&pid=ImgRaw&r=0" },
                new Juego { Titulo = "Spider-Man 2", ImagenUrl = "https://th.bing.com/th/id/R.b1370f1b7368d8e876d64e159e5f1d56?rik=NTY44blNsUxi%2fA&pid=ImgRaw&r=0" },
                new Juego { Titulo = "Hogwarts Legacy", ImagenUrl = "https://th.bing.com/th/id/R.b1370f1b7368d8e876d64e159e5f1d56?rik=NTY44blNsUxi%2fA&pid=ImgRaw&r=0" }
            },
            ["Favoritos de la comunidad"] = new List<Juego>
            {
                new Juego { Titulo = "The Witcher 3", ImagenUrl = "https://th.bing.com/th/id/R.b1370f1b7368d8e876d64e159e5f1d56?rik=NTY44blNsUxi%2fA&pid=ImgRaw&r=0" },
                new Juego { Titulo = "Cyberpunk 2077", ImagenUrl = "https://th.bing.com/th/id/R.b1370f1b7368d8e876d64e159e5f1d56?rik=NTY44blNsUxi%2fA&pid=ImgRaw&r=0" }
            }
        };
    }

    
}