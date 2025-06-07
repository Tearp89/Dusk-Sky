using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UI.Pages;
public class Juego
{
    public string Titulo { get; set; }
    public string ImagenUrl { get; set; }
    public string DescripcionCorta { get; set; }

    public string Usuario { get; set; }

    public string AvatarUrl { get; set; } = "https://i.pravatar.cc/32";
public string UsuarioAvatarUrl { get; set; } = string.Empty;
public string UsuarioNombre { get; set; } = string.Empty;
public double Rating { get; set; } // Usa float si lo prefieres
public DateTime Fecha { get; set; }

}

public class Comentario
{
    public string Usuario { get; set; }
    public string Texto { get; set; }
    public DateTime Fecha { get; set; }
    public string AvatarUrl { get; set; } = "https://i.pravatar.cc/32";
    public int Likes { get; set; }
}

public class Usuario {
    public string NombreUsuario { get; set; }
    public string AvatarUrl { get; set; } = "https://i.pravatar.cc/32";


}

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    public Dictionary<string, List<Juego>> SeccionesJuegos { get; set; }

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
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
    new Juego {
        Titulo = "The Last of Us Part II",
        ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain",
        DescripcionCorta = "Una intensa historia de venganza.",
        UsuarioAvatarUrl = "/images/avatars/avatar1.png",
        UsuarioNombre = "Ellie",
        Rating = 4.5,
        Fecha = DateTime.Today.AddDays(-1)
    },
    new Juego {
        Titulo = "Ghost of Tsushima",
        ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain",
        DescripcionCorta = "Un samurái en la isla de Tsushima.",
        UsuarioAvatarUrl = "/images/avatars/avatar2.png",
        UsuarioNombre = "Jin",
        Rating = 5.0,
        Fecha = DateTime.Today.AddDays(-2)
    },
    new Juego {
        Titulo = "Marvel's Spider-Man: Miles Morales",
        ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain",
        DescripcionCorta = "La nueva aventura de Spider-Man.",
        UsuarioAvatarUrl = "/images/avatars/avatar3.png",
        UsuarioNombre = "Miles",
        Rating = 4.0,
        Fecha = DateTime.Today.AddDays(-3)
    },
    new Juego {
        Titulo = "Horizon Forbidden West",
        ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain",
        DescripcionCorta = "Aloy viaja al oeste prohibido.",
        UsuarioAvatarUrl = "/images/avatars/avatar4.png",
        UsuarioNombre = "Aloy",
        Rating = 4.8,
        Fecha = DateTime.Today.AddDays(-4)
    },
    new Juego {
        Titulo = "Elden Ring",
        ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain",
        DescripcionCorta = "Un nuevo mundo de fantasía oscura.",
        UsuarioAvatarUrl = "/images/avatars/avatar5.png",
        UsuarioNombre = "Tarnished",
        Rating = 4.9,
        Fecha = DateTime.Today.AddDays(-5)
    },
    new Juego {
        Titulo = "Cyberpunk 2077",
        ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain",
        DescripcionCorta = "Un RPG de mundo abierto en Night City.",
        UsuarioAvatarUrl = "/images/avatars/avatar6.png",
        UsuarioNombre = "V",
        Rating = 3.7,
        Fecha = DateTime.Today.AddDays(-6)
    },
    new Juego {
        Titulo = "God of War Ragnarök",
        ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain",
        DescripcionCorta = "La continuación de la saga nórdica.",
        UsuarioAvatarUrl = "/images/avatars/avatar7.png",
        UsuarioNombre = "Kratos",
        Rating = 5.0,
        Fecha = DateTime.Today.AddDays(-7)
    },
    new Juego {
        Titulo = "Gran Turismo 7",
        ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain",
        DescripcionCorta = "La experiencia de conducción real.",
        UsuarioAvatarUrl = "/images/avatars/avatar8.png",
        UsuarioNombre = "RacerX",
        Rating = 4.2,
        Fecha = DateTime.Today.AddDays(-8)
    },
    new Juego {
        Titulo = "Returnal",
        ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain",
        DescripcionCorta = "Un shooter roguelike en un planeta hostil.",
        UsuarioAvatarUrl = "/images/avatars/avatar9.png",
        UsuarioNombre = "Selene",
        Rating = 4.3,
        Fecha = DateTime.Today.AddDays(-9)
    },
    new Juego {
        Titulo = "Ratchet & Clank: Rift Apart",
        ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain",
        DescripcionCorta = "Una aventura interdimensional.",
        UsuarioAvatarUrl = "/images/avatars/avatar10.png",
        UsuarioNombre = "Ratchet",
        Rating = 4.6,
        Fecha = DateTime.Today.AddDays(-10)
    },
    new Juego {
        Titulo = "Final Fantasy VII Remake Intergrade",
        ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain",
        DescripcionCorta = "Una reimaginación del clásico RPG.",
        UsuarioAvatarUrl = "/images/avatars/avatar11.png",
        UsuarioNombre = "Cloud",
        Rating = 4.7,
        Fecha = DateTime.Today.AddDays(-11)
    },
    new Juego {
        Titulo = "Stray",
        ImagenUrl = "https://th.bing.com/th/id/OIP.5BMB4SugaGRv0v7doXR1AgHaEK?rs=1&pid=ImgDetMain",
        DescripcionCorta = "Un gato perdido en una ciudad cibernética.",
        UsuarioAvatarUrl = "/images/avatars/avatar12.png",
        UsuarioNombre = "Catlover",
        Rating = 4.4,
        Fecha = DateTime.Today.AddDays(-12)
    },
};

Juegos = juegosSimulados.Take(12).ToList();

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
