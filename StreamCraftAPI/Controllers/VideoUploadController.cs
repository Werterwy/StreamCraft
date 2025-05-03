using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly WebSocketConnectionManager _webSocketConnectionManager;
        private readonly VideoProcessingService _videoService;

        public VideoUploadController(VideoStorageService storageService, StreamCraftDbContext dbContext,
            VideoProcessingQueue queue, ILogger<VideoUploadController> logger, WebSocketConnectionManager webSocketConnectionManager,
            VideoProcessingService videoService)
        {
            _storageService = storageService;
            _dbContext = dbContext;
            _queue = queue;
            _logger = logger;
            _webSocketConnectionManager = webSocketConnectionManager;
            _videoService = videoService;
        }
        [RequestSizeLimit(100_000_000_000)]
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
                UpdatedAt = null
            };

            _logger.LogInformation("Uploading file {FileName} by user {UserId}", file.FileName, userId);

            _dbContext.Videos.Add(video);
            _dbContext.VideoProcessingTasks.Add(processingTask);
            await _dbContext.SaveChangesAsync();

            await _webSocketConnectionManager.SendToUser(userId.ToString(), "Видео готово");

            _logger.LogInformation("Video {VideoId} and processing task created.", videoId);

            await _queue.EnqueueAsync(processingTask.Id); 

            _logger.LogInformation("Task {TaskId} enqueued for processing.", processingTask.Id);

            return Ok(new { video.Id, file.FileName, Status = "Uploaded" });
        }

        [HttpPost("process-video")]
        public async Task<IActionResult> ProcessVideo([FromQuery] string userId)
        {
            await _videoService.ProcessVideoAndNotify(userId);
            return Ok("Процесс запущен");
        }

        [HttpGet("status/{taskId}")]
        public async Task<IActionResult> GetVideoProcessingStatus(Guid taskId)
        {
            var task = await _dbContext.VideoProcessingTasks
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound("Task not found");

            return Ok(new
            {
                Status = task.Status.ToString(),
                Attempts = task.Attempts,
                ErrorMessage = task.ErrorMessage
            });
        }


    }
}
