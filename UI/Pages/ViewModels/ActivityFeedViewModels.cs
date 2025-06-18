

public abstract class ActivityFeedItemViewModel
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } 
    public string UserId { get; set; } 
    public string Username { get; set; } 
    public string UserAvatarUrl { get; set; } 
}

public class ReviewActivityViewModel : ActivityFeedItemViewModel
{
    public string ReviewId { get; set; }
    public string GameId { get; set; }
    public string GameTitle { get; set; }
    public string GameImageUrl { get; set; }
    public string Content { get; set; }
    public double Rating { get; set; }
    public int LikesCount { get; set; }
}

public class GameLogActivityViewModel : ActivityFeedItemViewModel
{
    public Guid GameTrackingId { get; set; }
    public string GameId { get; set; }
    public string GameTitle { get; set; }
    public string GameImageUrl { get; set; }
    public string Status { get; set; } // Ej. Playing, Completed, Planning
    // Puedes añadir más propiedades específicas del log si GameTrackingDto las tiene
}

public class GameListActivityViewModel : ActivityFeedItemViewModel
{
    public string ListId { get; set; }
    public string ListName { get; set; }
    public string? Description { get; set; } // Si tu GameListDTO tiene descripción
    public int ItemCount { get; set; } // Número de elementos en la lista
}

// Puedes poner esto en tu archivo ActivityFeedViewModels.cs o en un nuevo LikesViewModels.cs
public class LikedGameViewModel
{
    public string GameId { get; set; } // Puede ser string o Guid, dependiendo de cómo lo uses en la vista
    public string GameTitle { get; set; } = string.Empty;
    public string GameImageUrl { get; set; } = string.Empty;
}
// *** IMPORTANTE: NECESITO LA DEFINICIÓN DE TUS DTOs ***
// Para adaptar GameLogActivityViewModel y GameListActivityViewModel correctamente,
// por favor, muéstrame las definiciones de:
// public class GameTrackingDto { ... }
// public class GameListDTO { ... }
// public class ReviewDTO { ... } // Aunque ya usaste ReviewCardViewModel, es bueno confirmar las propiedades aquí también.
// public class GamePreviewDTO { ... } // Para GameId, Title, HeaderUrl