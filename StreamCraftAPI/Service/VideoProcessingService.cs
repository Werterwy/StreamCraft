using StreamCraftAPI.Data.DbContext;
using StreamCraftAPI.Data.Model;
using StreamCraftAPI.Queues;
using System.Diagnostics;

namespace StreamCraftAPI.Service
{
    public class VideoProcessingService : BackgroundService
    {
        private readonly VideoProcessingQueue _queue;
        private readonly IServiceProvider _serviceProvider;

        public VideoProcessingService(VideoProcessingQueue queue, IServiceProvider serviceProvider)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var videoId = _queue.Dequeue();

                if (videoId != null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<StreamCraftDbContext>();
                    var video = await dbContext.Videos.FindAsync(videoId);

                    if (video != null)
                    {
                        // TODO: Тут будет обработка видео (конвертация, миниатюры, водяной знак)
                        video.Status = (int)VideoStatus.Processed;
                        await dbContext.SaveChangesAsync();
                    }
                }
                else
                {
                    await Task.Delay(1000, stoppingToken); // подождать, если нет задач
                }
            }
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
