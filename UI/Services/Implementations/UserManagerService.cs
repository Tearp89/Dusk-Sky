public class UserManagerService : IUserManagerService
{
    private readonly HttpClient _http;

    public UserManagerService(HttpClient http)
    {
        _http = http;
    }

    public async Task<UserProfileDTO?> GetProfileAsync(string userId)
    {
        var response = await _http.GetAsync($"{userId}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<UserProfileDTO>()
            : null;
    }

    public async Task<bool> CreateProfileAsync(string userId, UserProfileCreateDTO payload)
    {
        var response = await _http.PutAsJsonAsync($"{userId}", payload);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteProfileAsync(string userId)
    {
        var response = await _http.DeleteAsync($"{userId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ChangeUsernameAsync(string userId, string newUsername)
    {
        var request = new { new_username = newUsername };
        var response = await _http.PatchAsJsonAsync($"{userId}/username", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<UserProfileDTO?> UploadProfileContentAsync(string userId, UserProfileUploadDTO uploadData)
    {
        using var content = new MultipartFormDataContent();

        if (uploadData.Avatar != null)
            content.Add(new StreamContent(uploadData.Avatar.OpenReadStream()), "avatar", uploadData.Avatar.FileName);

        if (uploadData.Banner != null)
            content.Add(new StreamContent(uploadData.Banner.OpenReadStream()), "banner", uploadData.Banner.FileName);

        if (uploadData.Media != null)
        {
            foreach (var file in uploadData.Media)
            {
                content.Add(new StreamContent(file.OpenReadStream()), "media", file.FileName);
            }
        }

        if (!string.IsNullOrEmpty(uploadData.Bio))
            content.Add(new StringContent(uploadData.Bio), "bio");

        if (!string.IsNullOrEmpty(uploadData.AboutSection))
            content.Add(new StringContent(uploadData.AboutSection), "about_section");

        var response = await _http.PatchAsync($"{userId}/upload", content);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<UserProfileDTO>()
            : null;
    }
}