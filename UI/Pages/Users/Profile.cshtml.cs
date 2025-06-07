using Microsoft.AspNetCore.Mvc.RazorPages;

public class ProfileModel : PageModel
{
    public string UserName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string SocialLink { get; set; } = string.Empty;
    public string TwitterLink { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public int TotalGames { get; set; }
    public int TotalFollowing { get; set; }
    public int TotalFollowers { get; set; }
    public Dictionary<string, List<CollageGame>> SeccionesJuegos { get; set; } = new();



    public List<Movie> FavoriteMovies { get; set; } = new();
    public List<UserActivity> RecentActivities { get; set; } = new();
    public List<Review> RecentReviews { get; set; } = new();
    public List<UserList> UserLists { get; set; } = new();
    public List<User> Following { get; set; } = new();
    public List<GameProfile> FavoriteGames { get; private set; }
    public List<WatchlistGame> Watchlist { get; set; } = new();
    



    public void OnGet()
    {
        // Datos simulados para ver el perfil lleno
        UserName = "Swizzy13";
        Location = "Veracruz, México";
        SocialLink = "https://dusk-sky.app/profile/swizzy13";
        TwitterLink = "https://twitter.com/swizzy13";
        AvatarUrl = "https://www.pinclipart.com/picdir/big/75-752081_the-workforce-diversity-network-welcomes-new-members-workplace.png"; // Ruta a la imagen del usuario
        Bio = "Amante de los juegos pixel art y los RPG japoneses.";
        TotalGames = 42;
        TotalFollowing = 12;
        TotalFollowers = 8;
        FavoriteMovies = new()
        {
            new Movie { Title = "Interstellar", PosterUrl = "https://th.bing.com/th/id/OIP.L85KxjYSYGrehVz9hZGXbwHaEN?o=7&cb=iwp2rm=3&rs=1&pid=ImgDetMain" },
            new Movie { Title = "Perfect Blue", PosterUrl = "https://th.bing.com/th/id/OIP.IxttfG-Rkss5r6fw8CTh8wHaEK?o=7&cb=iwp2rm=3&rs=1&pid=ImgDetMain" },
            new Movie { Title = "Drive", PosterUrl = "https://th.bing.com/th/id/OIP.gcyg1HOJw1jPe6qu23a0jwHaEK?o=7&cb=iwp2rm=3&rs=1&pid=ImgDetMain" }
        };

        RecentActivities = new()
        {
            new() { GameId = 1, Title = "Sotsugyou", CoverUrl = "https://th.bing.com/th/id/OIP.8DIkfAqrLApv8WW4J6ijYgHaIb?o=7&cb=iwp2rm=3&rs=1&pid=ImgDetMain", Rating = 4.0, Liked = false, Reviewed = false },
            new() { GameId = 2, Title = "Happy End", CoverUrl = "https://th.bing.com/th/id/OIP.8DIkfAqrLApv8WW4J6ijYgHaIb?o=7&cb=iwp2rm=3&rs=1&pid=ImgDetMain", Rating = 4.5, Liked = true, Reviewed = true },
            new() { GameId = 3, Title = "Mickey 17", CoverUrl = "https://th.bing.com/th/id/OIP.8DIkfAqrLApv8WW4J6ijYgHaIb?o=7&cb=iwp2rm=3&rs=1&pid=ImgDetMain", Rating = 4.0, Liked = false, Reviewed = true },
            new() { GameId = 4, Title = "The Boy, the Mole...", CoverUrl = "https://th.bing.com/th/id/OIP.8DIkfAqrLApv8WW4J6ijYgHaIb?o=7&cb=iwp2rm=3&rs=1&pid=ImgDetMain", Rating = 2.5, Liked = false, Reviewed = true },
        };

        Watchlist = new List<WatchlistGame>
    {
        new() { Id = 1, Title = "From the End of the World", CoverUrl = "https://th.bing.com/th/id/OIP.3IVQxP6g7yCAaw1pEwx44AHaEo?rs=1&pid=ImgDetMain" },
        new() { Id = 2, Title = "Cats Piano", CoverUrl = "https://th.bing.com/th/id/OIP.3IVQxP6g7yCAaw1pEwx44AHaEo?rs=1&pid=ImgDetMain" },
        new() { Id = 3, Title = "Spring Breakers", CoverUrl = "https://th.bing.com/th/id/OIP.3IVQxP6g7yCAaw1pEwx44AHaEo?rs=1&pid=ImgDetMain" },
        new() { Id = 4, Title = "Arrival", CoverUrl = "https://th.bing.com/th/id/OIP.3IVQxP6g7yCAaw1pEwx44AHaEo?rs=1&pid=ImgDetMain" },
        new() { Id = 5, Title = "The Endless", CoverUrl = "https://th.bing.com/th/id/OIP.3IVQxP6g7yCAaw1pEwx44AHaEo?rs=1&pid=ImgDetMain" },
    };



        UserLists = new()
        {
            new UserList { Title = "Mis joyas ocultas", Description = "Películas que casi nadie menciona, pero amo." },
            new UserList { Title = "Para llorar bonito", Description = "Las que me rompen pero me hacen feliz." }
        };

        Following = new()
        {
            new User { UserName = "FerRM", AvatarUrl = "https://th.bing.com/th/id/OIP.41WoUsFM1YH118L7NESmiwHaHa?o=7&cb=iwp2rm=3&rs=1&pid=ImgDetMain" },
            new User { UserName = "SoraUta", AvatarUrl = "https://th.bing.com/th/id/OIP.KwnNrvjJIf5PsNKTLaYELAHaHa?cb=iwp2&w=703&h=703&rs=1&pid=ImgDetMain" }
        };

        FavoriteGames = new List<GameProfile>
        {
            new GameProfile { Id = 1, Title = "Mysterious Skin", CoverUrl = "https://th.bing.com/th/id/OIP.KkHA0OyFCnEY5oyj_z500AHaLC?cb=iwp2&rs=1&pid=ImgDetMain" },
            new GameProfile { Id = 2, Title = "Gran Torino", CoverUrl = "https://juicyreviewz.files.wordpress.com/2014/11/gran-torino-poster-artwork-clint-eastwood-bee-vang-ahney-her.jpg" },
            new GameProfile { Id = 3, Title = "Scherzo", CoverUrl = "https://th.bing.com/th/id/OIP.l72XuS7_Jz3Wl7Kpc4WtMAHaKk?o=7&cb=iwp2rm=3&rs=1&pid=ImgDetMain" },
            new GameProfile { Id = 4, Title = "All About Lily Chou-Chou", CoverUrl = "https://th.bing.com/th/id/R.a206b53e23f85b99addeb48ab8f43a17?rik=icHvTj9I9mLs9A&pid=ImgRaw&r=0" }
        };

        SeccionesJuegos = new Dictionary<string, List<CollageGame>>
        {
            ["Played Recently"] = new()
    {
        new() { Titulo = "Dark Souls", DescripcionCorta = "Challenging action RPG", ImagenUrl = "/images/games/darksouls.jpg" },
        new() { Titulo = "Celeste", DescripcionCorta = "Precision platformer", ImagenUrl = "/images/games/celeste.jpg" },
        new() { Titulo = "Hollow Knight", DescripcionCorta = "Metroidvania", ImagenUrl = "/images/games/hollowknight.jpg" }
    }
        };


        RecentReviews = new List<Review>
{
    new() {
        Id = 1,
        GameId = 100,
        GameTitle = "My Pretend Girlfriend",
        CoverUrl = "/images/pretend.jpg",
        Year = 2014,
        Rating = 4.0,
        WatchedDate = new DateTime(2025, 4, 24),
        Content = "Un crudo final, pensé que tendría el típico final pero no",
        ContainsSpoilers = false
    },
    new() {
        Id = 2,
        GameId = 101,
        GameTitle = "Happyend",
        CoverUrl = "/images/happyend.jpg",
        Year = 2024,
        Rating = 4.5,
        WatchedDate = new DateTime(2025, 4, 15),
        Content = "This review may contain spoilers. <em>I can handle the truth.</em>",
        ContainsSpoilers = true
    }
};




    }
}

public class Movie
{
    public string Title { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
}

public class UserActivity
{
    public int GameId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public double Rating { get; set; } 
    public bool Liked { get; set; }
    public bool Reviewed { get; set; }
    
    public int ReviewId { get; set; }
}

public class Review
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string GameTitle { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public int Year { get; set; }
    public double Rating { get; set; }
    public DateTime WatchedDate { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool ContainsSpoilers { get; set; }
}

public class UserList
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class User
{
    public string UserName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}

public class GameProfile
{
    public int Id { get; set; }            // o usa string Slug si prefieres URLs amigables
    public string Title { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
}



public class CollageGame
{
    public string Titulo { get; set; } = string.Empty;
    public string DescripcionCorta { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
}

public class WatchlistGame
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
}