public class UserSessionManager
{
    private static UserSessionManager? _instance;

    public static UserSessionManager Instance => _instance ??= new UserSessionManager();

    private UserSessionManager() { }

    public string? AccessToken { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? UserId { get; set; }
    public string? Role { get; set; }
}
