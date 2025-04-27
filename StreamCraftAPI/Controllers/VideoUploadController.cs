using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using StreamCraftAPI.Data.DbContext;
using StreamCraftAPI.Data.Entities;
using StreamCraftAPI.Data.Model;
using StreamCraftAPI.Queues;
using StreamCraftAPI.Service;

namespace StreamCraftAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoUploadController : ControllerBase
    {
        private readonly VideoStorageService _storageService;
        private readonly StreamCraftDbContext _dbContext;
        private readonly VideoProcessingQueue _queue;
        private readonly ILogger<VideoUploadController> _logger;

        public VideoUploadController(VideoStorageService storageService, StreamCraftDbContext dbContext, VideoProcessingQueue queue, ILogger<VideoUploadController> logger)
        {
            _storageService = storageService;
            _dbContext = dbContext;
            _queue = queue;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadVideo(IFormFile file, Guid userId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file.");

            var filePath = await _storageService.SaveTempVideoAsync(file);

            var video = new Video
            {
                Id = Guid.NewGuid(),                
                UserId = userId,        
                FilePath = filePath,                 
                ThumbnailPath = null,               
                Status = VideoStatus.Uploaded,      
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Uploading file {FileName} by user {UserId}", file.FileName, userId);

            _dbContext.Videos.Add(video);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Video {VideoId} saved to database.", video.Id);

            _queue.Enqueue(video.Id);

            _logger.LogInformation("Video {VideoId} enqueued for processing.", video.Id);

            return Ok(new { video.Id, file.FileName, Status = "Uploaded" });
        }
    }
}
