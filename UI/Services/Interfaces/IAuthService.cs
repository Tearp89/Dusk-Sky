public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(AuthRequestDto request);
    Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto dto);
    Task<List<UserSearchResultDto>> SearchUsersAsync(string partialUsername);
    Task<UserSearchResultDto> SearchUserByIdAsync(string userId);
    Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword); // Para POST /change-password/{user_id}
    Task<bool> DeleteAccountAsync(string userId); 
    Task<bool> UpdateUsernameAsync(string userId, string newUsername);
    Task<bool> PromoteUserAsync(string userId); 
    Task<bool> DemoteUserAsync(string userId);
}
