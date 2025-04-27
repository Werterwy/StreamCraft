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
                        video.Status = VideoStatus.Processed;
                        await dbContext.SaveChangesAsync();
                    }
                }
                else
                {
                    await Task.Delay(1000, stoppingToken); // подождать, если нет задач
                }
            }
        }



    }
}
