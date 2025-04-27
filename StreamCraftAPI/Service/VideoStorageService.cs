using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace StreamCraftAPI.Service
{
    public class VideoStorageService
    {
        private readonly string _tempFolderPath;

        public VideoStorageService(IWebHostEnvironment env)
        {
            _tempFolderPath = Path.Combine(env.WebRootPath, "tempVideos");
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

        public async Task ConvertVideoAsync(string inputPath, string outputPath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{inputPath}\" -c:v libx264 -preset fast -crf 23 \"{outputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
        }

        public async Task CreateThumbnailAsync(string inputPath, string thumbnailPath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{inputPath}\" -ss 00:00:01.000 -vframes 1 \"{thumbnailPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
        }
    }
}
