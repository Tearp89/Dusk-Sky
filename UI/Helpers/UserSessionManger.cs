public class UserSessionManager
{
    private static readonly Lazy<UserSessionManager> _instance = new(() => new UserSessionManager());

    public static UserSessionManager Instance => _instance.Value;

    public UserSessionManager() { }

    public string? Token { get; private set; }
    public string? Username { get; private set; }
    public string? UserId { get; private set; }

    public void SetSession(string token, string userId, string username)
    {
        Token = token;
        UserId = userId;
        Username = username;
    }

    public void ClearSession()
    {
        Token = null;
        UserId = null;
        Username = null;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
}
