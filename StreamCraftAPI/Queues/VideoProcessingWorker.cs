using StreamCraftAPI.Service;

namespace StreamCraftAPI.Queues
{
    public class VideoProcessingWorker : BackgroundService
    {
        private readonly VideoProcessingQueue _queue;
        private readonly VideoStorageService _storageService;
        private readonly ILogger<VideoProcessingWorker> _logger;

        public VideoProcessingWorker(VideoProcessingQueue queue, VideoStorageService storageService, ILogger<VideoProcessingWorker> logger)
        {
            _queue = queue;
            _storageService = storageService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Video Processing Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var videoId = _queue.Dequeue();

                if (videoId != null)
                {
                    try
                    {
                        // Здесь тебе нужно будет получить путь к файлу по videoId из БД
                        var inputPath = GetInputVideoPath(videoId.Value);
                        var outputPath = GetConvertedVideoPath(videoId.Value);
                        var thumbnailPath = GetThumbnailPath(videoId.Value);

                        await _storageService.ConvertVideoAsync(inputPath, outputPath);
                        await _storageService.CreateThumbnailAsync(outputPath, thumbnailPath);

                        // Здесь можно обновить статус видео в БД
                        UpdateVideoStatusToProcessed(videoId.Value);

                        _logger.LogInformation($"Video {videoId} processed successfully.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing video {videoId}");
                        // Можно сохранить информацию об ошибке в БД
                    }
                }
                else
                {
                    // Если очередь пуста, подождём пару секунд
                    await Task.Delay(2000, stoppingToken);
                }
            }

            _logger.LogInformation("Video Processing Worker stopping.");
        }

        private string GetInputVideoPath(Guid videoId)
        {
            // Здесь будет код для получения пути к исходному видео из БД
            return Path.Combine("wwwroot", "tempVideos", $"{videoId}.mp4");
        }

        private string GetConvertedVideoPath(Guid videoId)
        {
            // Пример для конвертированного видео
            return Path.Combine("wwwroot", "videos", $"{videoId}_converted.mp4");
        }

        private string GetThumbnailPath(Guid videoId)
        {
            // Пример для превью-картинки
            return Path.Combine("wwwroot", "thumbnails", $"{videoId}_thumb.jpg");
        }

        private void UpdateVideoStatusToProcessed(Guid videoId)
        {
            // Здесь обновляешь статус в БД, например "Processed"
        }
    }
}
