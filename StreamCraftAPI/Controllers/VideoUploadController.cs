using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using StreamCraftAPI.Data.DbContext;
using StreamCraftAPI.Data.Entities;
using StreamCraftAPI.Service;

namespace StreamCraftAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoUploadController : ControllerBase
    {
        private readonly VideoStorageService _storageService;
        private readonly StreamCraftDbContext _dbContext;

        public VideoUploadController(VideoStorageService storageService, StreamCraftDbContext dbContext)
        {
            _storageService = storageService;
            _dbContext = dbContext;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadVideo(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file.");

            var filePath = await _storageService.SaveTempVideoAsync(file);

            var video = new Video
            {
                FileName = file.FileName,
                TempPath = filePath,
                UploadTime = DateTime.UtcNow,
                Status = "Uploaded"
            };

            _dbContext.Videos.Add(video);
            await _dbContext.SaveChangesAsync();

            return Ok(new { video.Id, video.FileName, Status = "Uploaded" });
        }
    }
}
