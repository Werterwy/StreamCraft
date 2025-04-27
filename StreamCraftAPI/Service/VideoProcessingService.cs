using StreamCraftAPI.Data.DbContext;
using StreamCraftAPI.Data.Model;
using StreamCraftAPI.Queues;
using System.Diagnostics;

namespace StreamCraftAPI.Service
{
    public class VideoProcessingService : BackgroundService
    {
        private readonly VideoProcessingQueue _queue;
        private readonly VideoStorageService _storageService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VideoProcessingService> _logger;

        public VideoProcessingService(VideoProcessingQueue queue, IServiceProvider serviceProvider, ILogger<VideoProcessingService> logger, VideoStorageService storageService)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _storageService = storageService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Starting video processing service.");

                var videoId = _queue.Dequeue();

                if (videoId != null)
                {
                    /*using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<StreamCraftDbContext>();
                    var video = await dbContext.Videos.FindAsync(videoId);

                    if (video != null)
                    {
                        // TODO: Тут будет обработка видео (конвертация, миниатюры, водяной знак)
                        video.Status = VideoStatus.Processed;
                        await dbContext.SaveChangesAsync();

                        _logger.LogInformation("Video {VideoId} processed successfully.", videoId);
                    }
                    else
                    {
                        _logger.LogWarning("Video {VideoId} not found.", videoId);
                    }*/

                    try
                    {
                        var inputPath = GetInputVideoPath(videoId.Value);
                        var outputPath = GetConvertedVideoPath(videoId.Value);
                        var thumbnailPath = GetThumbnailPath(videoId.Value);

                        await _storageService.ConvertVideoAsync(inputPath, outputPath);
                        await _storageService.CreateThumbnailAsync(outputPath, thumbnailPath);

                        UpdateVideoStatusToProcessed(videoId.Value);

                        _logger.LogInformation($"Video {videoId} processed successfully.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing video {videoId}");
                    }
                }
                else
                {
                    await Task.Delay(1000, stoppingToken); // подождать, если нет задач
                }
            }
        }

        private string GetInputVideoPath(Guid videoId)
        {
            return Path.Combine("wwwroot", "tempVideos", $"{videoId}.mp4");
        }

        private string GetConvertedVideoPath(Guid videoId)
        {
            return Path.Combine("wwwroot", "videos", $"{videoId}_converted.mp4");
        }

        private string GetThumbnailPath(Guid videoId)
        {
            return Path.Combine("wwwroot", "thumbnails", $"{videoId}_thumb.jpg");
        }

        private void UpdateVideoStatusToProcessed(Guid videoId)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<StreamCraftDbContext>();

            var video = dbContext.Videos.Find(videoId);
            if (video != null)
            {
                video.Status = VideoStatus.Processed;
                dbContext.SaveChanges();
            }

        }



    }
}
