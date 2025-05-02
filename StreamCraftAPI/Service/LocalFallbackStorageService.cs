using StreamCraftAPI.Interface;

namespace StreamCraftAPI.Service
{
    public class LocalFallbackStorageService : IPermanentStorageService
    {
        private readonly string _videosPath;
        private readonly string _thumbnailsPath;
        private readonly string _cdnBaseUrl;
        private readonly ILogger<LocalFallbackStorageService> _logger;

        public LocalFallbackStorageService(IWebHostEnvironment env, ILogger<LocalFallbackStorageService> logger)
        {
            _videosPath = Path.Combine(env.WebRootPath, "fallbackVideos");
            _thumbnailsPath = Path.Combine(env.WebRootPath, "fallbackThumbnails");
            _cdnBaseUrl = "https://cdn.myproject.com:5001";
            _logger = logger;

            Directory.CreateDirectory(_videosPath);
            Directory.CreateDirectory(_thumbnailsPath);
        }

        public async Task<string> UploadVideoAsync(string localFilePath, string fileName)
        {
            var destPath = Path.Combine(_videosPath, fileName);
            File.Copy(localFilePath, destPath, overwrite: true);

            _logger.LogWarning("Video saved to local fallback: {Path}", destPath);
            return $"{_cdnBaseUrl}/fallbackVideos/{fileName}";
        }

        public async Task<string> UploadThumbnailAsync(string localFilePath, string fileName)
        {
            var destPath = Path.Combine(_thumbnailsPath, fileName);
            File.Copy(localFilePath, destPath, overwrite: true);

            _logger.LogWarning("Thumbnail saved to local fallback: {Path}", destPath);
            return $"{_cdnBaseUrl}/fallbackThumbnails/{fileName}";
        }
    }

}
