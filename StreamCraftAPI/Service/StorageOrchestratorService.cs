using StreamCraftAPI.Data.DbContext;
using StreamCraftAPI.Interface;
using System;

namespace StreamCraftAPI.Service
{
    public class StorageOrchestratorService
    {
        private readonly IPermanentStorageService _permanentStorage;
        private readonly StreamCraftDbContext _dbContext;
        private readonly ILogger<StorageOrchestratorService> _logger;

        public StorageOrchestratorService(IPermanentStorageService permanentStorage, StreamCraftDbContext dbContext, ILogger<StorageOrchestratorService> logger)
        {
            _permanentStorage = permanentStorage;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task StoreAndUpdateVideoAsync(Guid videoId, string local720Path, string local1080Path, string localThumbnailPath)
        {
            var video = await _dbContext.Videos.FindAsync(videoId);
            if (video == null)
                throw new Exception("Video not found");

            video.FilePath720 = await _permanentStorage.UploadVideoAsync(local720Path, $"{videoId}_720.mp4");
            video.FilePath1080 = await _permanentStorage.UploadVideoAsync(local1080Path, $"{videoId}_1080.mp4");
            video.ThumbnailPath = await _permanentStorage.UploadThumbnailAsync(localThumbnailPath, $"{videoId}.jpg");

            _logger.LogInformation("CDN URLs saved: 720={Url720}, 1080={Url1080}", video.FilePath720, video.FilePath1080);
            await _dbContext.SaveChangesAsync();
        }
    }

}
