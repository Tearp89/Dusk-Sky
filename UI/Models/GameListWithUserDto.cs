    public class GameListWithUserDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public string UserId { get; set; } = default!;
    public DateTime Date { get; set; }

    // Usuario
    public string UserName { get; set; } = "Usuario desconocido";
    public string AvatarUrl { get; set; } = "/Images/noImage.png";

    // Juego principal
    public string? GameId { get; set; }
    public List<string> GameHeaders { get; set; } // Lista de URLs de juegos
}
