// Archivo: ViewModels/ProfileViewModels.cs

// Para el encabezado del perfil
public class ProfileHeaderViewModel
{
    public string UserId { get; set; }
    public string Username { get; set; }
    public string AvatarUrl { get; set; }
    public string BannerUrl { get; set; }
    public string Bio { get; set; }
    public int ReviewCount { get; set; }
    public int ListCount { get; set; }
    public int FriendCount { get; set; }
}

// Para mostrar un amigo en una lista
public class FriendViewModel
{
    public string UserId { get; set; }
    public string Username { get; set; }
    public string AvatarUrl { get; set; }
}

public class FriendRequestViewModel
{
    public string RequestId { get; set; } // Needed for Accept/Reject actions
    public string UserId { get; set; }
    public string Username { get; set; }
    public string AvatarUrl { get; set; }
}

// Para mostrar una reseña en una tarjeta
public class ReviewCardViewModel
{
    public string ReviewId { get; set; }
    public string GameId { get; set; }
    public string GameTitle { get; set; }
    public string GameImageUrl { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string UserAvatarUrl { get; set; }
    public string Content { get; set; }
    public double Rating { get; set; }
    public int LikesCount { get; set; }
    public System.DateTime CreatedAt { get; set; }
}

public class CommentViewModel
    {
        public string UserName { get; set; } = "";
        public string UserAvatarUrl { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

public class ProfileGameViewModel
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string CoverUrl { get; set; }
}

public class ProfileActivityViewModel
{
    public string GameId { get; set; }
    public string Title { get; set; }
    public string CoverUrl { get; set; }
    public double Rating { get; set; }
    public bool Liked { get; set; }
    public bool Reviewed { get; set; }
    public string ReviewId { get; set; }
    public System.DateTime WatchedDate { get; set; }
}

