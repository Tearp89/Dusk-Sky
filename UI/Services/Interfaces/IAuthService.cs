public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(AuthRequestDto request);
    Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto dto);
}
