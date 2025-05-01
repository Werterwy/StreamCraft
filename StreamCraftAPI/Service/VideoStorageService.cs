using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace StreamCraftAPI.Service
{
    public class VideoStorageService
    {
        private readonly string _tempFolderPath;
        private readonly string _videosFolderPath;
        private readonly string _thumbnailsFolderPath;
        private readonly ILogger<VideoStorageService> _logger;

        private readonly string _wwwrootPath;

        public VideoStorageService(IWebHostEnvironment env, ILogger<VideoStorageService> logger)
        {
            _logger = logger;

            _tempFolderPath = Path.Combine(env.WebRootPath, "tempVideos");
            _videosFolderPath = Path.Combine(env.WebRootPath, "videos");
            _thumbnailsFolderPath = Path.Combine(env.WebRootPath, "thumbnails");
            _wwwrootPath = env.WebRootPath;

            EnsureDirectory(_tempFolderPath);
            EnsureDirectory(_videosFolderPath);
            EnsureDirectory(_thumbnailsFolderPath);
        }

        public async Task<string> SaveTempVideoAsync(IFormFile file)
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var fullPath = Path.Combine(_tempFolderPath, uniqueFileName);

            _logger.LogInformation("Saving uploaded video to: {Path}", fullPath);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fullPath;
        }

        public async Task<(string path720, string path1080)> ConvertVideoAsync(string inputPath, string outputDirectory)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException("Input video not found", inputPath);

            _logger.LogInformation("Starting video conversion with watermark: {Input}", inputPath);

            var baseName = Path.GetFileNameWithoutExtension(inputPath);
            var path720 = Path.Combine(outputDirectory, $"{baseName}_720p.mp4");
            var path1080 = Path.Combine(outputDirectory, $"{baseName}_1080p.mp4");

            string watermarkText = "MyService"; 

            var args720 = $"-i \"{inputPath}\" -vf \"scale=-2:720,drawtext=text='{watermarkText}':x=10:y=10:fontsize=24:fontcolor=white:shadowcolor=black:shadowx=2:shadowy=2\" " +
                          "-c:v libx264 -preset fast -crf 23 -c:a copy " +
                          $"\"{path720}\"";

            var args1080 = $"-i \"{inputPath}\" -vf \"scale=-2:1080,drawtext=text='{watermarkText}':x=10:y=10:fontsize=32:fontcolor=white:shadowcolor=black:shadowx=2:shadowy=2\" " +
                           "-c:v libx264 -preset fast -crf 23 -c:a copy " +
                           $"\"{path1080}\"";

            var success720 = await RunFfmpegProcess(args720);
            var success1080 = await RunFfmpegProcess(args1080);

            if (!success720 || !success1080)
                throw new Exception("One or both video conversions failed");

            _logger.LogInformation("Video converted successfully to 720p and 1080p with watermark");

            return (path720, path1080);
        }


        public async Task CreateThumbnailAsync(string inputPath, string thumbnailPath)
        {
            _logger.LogInformation("Creating thumbnail from: {Input} -> {Thumbnail}", inputPath, thumbnailPath);

            if (!File.Exists(inputPath))
            {
                _logger.LogError("Input file for thumbnail not found: {Path}", inputPath);
                throw new FileNotFoundException("Input video not found", inputPath);
            }

            var arguments = $"-i \"{inputPath}\" -ss 00:00:01.000 -vframes 1 \"{thumbnailPath}\"";

            var success = await RunFfmpegProcess(arguments);
            if (!success)
            {
                _logger.LogError("Failed to create thumbnail: {Input}", inputPath);
                throw new Exception("Thumbnail creation failed");
            }
        }

        private async Task<bool> RunFfmpegProcess(string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = arguments,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _logger.LogDebug("Running ffmpeg with arguments: {Args}", arguments);

            process.Start();

            var errorOutput = await process.StandardError.ReadToEndAsync();
            var exitCode = await Task.Run(() =>
            {
                process.WaitForExit();
                return process.ExitCode;
            });

            if (exitCode != 0)
            {
                _logger.LogError("ffmpeg exited with code {Code}. Error: {Error}", exitCode, errorOutput);
                return false;
            }

            _logger.LogDebug("ffmpeg completed successfully.");
            return true;
        }

        private void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.LogInformation("Created directory: {Path}", path);
            }
        }

        public string GetTempPath(Guid id) =>
        Path.Combine(_wwwrootPath, "tempVideos", $"{id}.mp4");

        public string GetVideoPath(Guid id) =>
            Path.Combine(_wwwrootPath, "videos", $"{id}_converted.mp4");

        public string GetThumbnailPath(Guid id) =>
            Path.Combine(_wwwrootPath, "thumbnails", $"{id}_thumb.jpg");

    }

}
