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

            var videoId = Guid.NewGuid();

            var video = new Video
            {
                Id = videoId,
                UserId = userId,
                FilePath = filePath,
                ThumbnailPath = null,
                CreatedAt = DateTime.UtcNow,
                FilePath720 = null,
                FilePath1080 = null
            };

            var processingTask = new VideoProcessingTask
            {
                Id = Guid.NewGuid(),
                VideoId = videoId,
                TaskType = VideoTaskType.Encode, 
                Status = StreamCraftAPI.Data.Model.TaskStatus.Pending,
                Attempts = 0,
                ErrorMessage = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Uploading file {FileName} by user {UserId}", file.FileName, userId);

            _dbContext.Videos.Add(video);
            _dbContext.VideoProcessingTasks.Add(processingTask);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Video {VideoId} and processing task created.", videoId);

            await _queue.EnqueueAsync(processingTask.Id); // enqueue task ID, not video ID

            _logger.LogInformation("Task {TaskId} enqueued for processing.", processingTask.Id);

            return Ok(new { video.Id, file.FileName, Status = "Uploaded" });
        }

    }
}
