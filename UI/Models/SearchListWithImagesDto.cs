public class SearchListWithImagesDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = "Unknown";
    public string AvatarUrl { get; set; } = "/Images/noImage.png";
    public List<string> GameHeaders { get; set; } = new();
    public bool IsPublic { get; set; } = false;
}
