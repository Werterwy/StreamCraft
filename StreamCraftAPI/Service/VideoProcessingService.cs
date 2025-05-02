using Microsoft.EntityFrameworkCore;
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
        private const int MaxAttempts = 3;

        public VideoProcessingService(VideoProcessingQueue queue, IServiceProvider serviceProvider, ILogger<VideoProcessingService> logger, VideoStorageService storageService)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _storageService = storageService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Video processing service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var videoId = await _queue.DequeueAsync(stoppingToken);
                    _logger.LogInformation("Dequeued video with ID: {VideoId}", videoId);

                    await ProcessVideoAsync(videoId, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in video processing loop.");
                }
            }

            _logger.LogInformation("Video processing service stopped.");
        }

        private async Task ProcessVideoAsync(Guid taskId, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<StreamCraftDbContext>();
            var orchestrator = scope.ServiceProvider.GetRequiredService<StorageOrchestratorService>();

            var task = await dbContext.VideoProcessingTasks
                .Include(t => t.Video)
                .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

            if (task == null)
            {
                _logger.LogWarning("Video processing task not found: {TaskId}", taskId);
                return;
            }

            var video = task.Video;

            task.Status = StreamCraftAPI.Data.Model.TaskStatus.Processing;
            task.Attempts++;
            task.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                var inputPath = _storageService.GetTempPath(video.Id);
                var outputPath = _storageService.GetVideoPath(video.Id);
                var thumbnailPath = _storageService.GetThumbnailPath(video.Id);

                var (path720, path1080) = await _storageService.ConvertVideoAsync(inputPath, outputPath);
                await _storageService.CreateThumbnailAsync(outputPath, thumbnailPath);

                // Загрузка файлов и обновление ссылок
                await orchestrator.StoreAndUpdateVideoAsync(video.Id, path720, path1080, thumbnailPath);

                video.FilePath = outputPath;
                video.FilePath720 = path720;
                video.FilePath1080 = path1080;
                video.ThumbnailPath = thumbnailPath;

                task.Status = StreamCraftAPI.Data.Model.TaskStatus.Completed;
                task.ErrorMessage = null;
                _logger.LogInformation("Video {VideoId} processed successfully.", video.Id);
            }
            catch (Exception ex)
            {
                task.ErrorMessage = ex.Message;
                task.UpdatedAt = DateTime.UtcNow;

                if (task.Attempts < MaxAttempts)
                {
                    _logger.LogWarning(ex, "Error processing video {VideoId}, will retry (Attempt {Attempt})", video.Id, task.Attempts);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    await _queue.EnqueueAsync(task.Id);
                    return;
                }

                task.Status = StreamCraftAPI.Data.Model.TaskStatus.Failed;
                _logger.LogError(ex, "Video {VideoId} failed after {Attempts} attempts.", video.Id, task.Attempts);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }


        /*protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting video processing service.");
            while (!stoppingToken.IsCancellationRequested)
            {
                var videoId = _queue.Dequeue();

                if (videoId != null)
                {
                    _logger.LogInformation("Processing video with ID: {VideoId}", videoId);
                    try
                    {
                        var inputPath = GetInputVideoPath(videoId.Value);
                        var outputPath = GetConvertedVideoPath(videoId.Value);
                        var thumbnailPath = GetThumbnailPath(videoId.Value);

                        await _storageService.ConvertVideoAsync(inputPath, outputPath);
                        await _storageService.CreateThumbnailAsync(outputPath, thumbnailPath);

                        await UpdateVideoStatusAsync(videoId.Value, outputPath, thumbnailPath);

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
            _logger.LogInformation("Video Processing Worker stopping.");
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

        private async Task UpdateVideoStatusAsync(Guid videoId, string videoPath, string thumbnailPath)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<StreamCraftDbContext>();

            var video = dbContext.Videos.Find(videoId);
            if (video == null)
            {
                _logger.LogWarning("Video not found in DB for ID: {VideoId}", videoId);
                return;
            }
            video.FilePath = videoPath;
            video.ThumbnailPath = thumbnailPath;
            video.Status = VideoStatus.Processed;
            await dbContext.SaveChangesAsync();
        }*/



    }
}
