public interface IUserManagerService
{
    Task<UserProfileDTO?> GetProfileAsync(string userId);
    Task<bool> CreateProfileAsync(string userId, UserProfileCreateDTO payload);
    Task<bool> DeleteProfileAsync(string userId);
    Task<bool> ChangeUsernameAsync(string userId, string newUsername);
    Task<UserProfileDTO?> UploadProfileContentAsync(string userId, UserProfileUploadDTO uploadData);
}