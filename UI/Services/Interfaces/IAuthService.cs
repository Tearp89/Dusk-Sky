public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(AuthRequestDto request);
    Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto dto);
    Task<List<UserSearchResultDto>> SearchUsersAsync(string partialUsername);
    Task<UserSearchResultDto> SearchUserByIdAsync(string userId);
}
