using Microsoft.AspNetCore.Http;

namespace StreamCraftAPI.Service
{
    public class VideoStorageService
    {
        private readonly string _tempFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tempVideos");

        public VideoStorageService()
        {
            if (!Directory.Exists(_tempFolderPath))
                Directory.CreateDirectory(_tempFolderPath);
        }

        public async Task<string> SaveTempVideoAsync(IFormFile file)
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var fullPath = Path.Combine(_tempFolderPath, uniqueFileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fullPath;
        }
    }
}
