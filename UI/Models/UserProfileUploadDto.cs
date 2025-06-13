public class UserProfileUploadDTO
    {
        public IFormFile? Avatar { get; set; }
        public IFormFile? Banner { get; set; }
        public List<IFormFile>? Media { get; set; }
        public string? Bio { get; set; }
        public string? AboutSection { get; set; }
    }