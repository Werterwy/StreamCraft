using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StreamCraftAPI.Service;
using System.Net.WebSockets;

namespace StreamCraftAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly WebSocketConnectionManager _connectionManager;
        private readonly ILogger<NotificationController> _logger;
        private readonly VideoProcessingService _videoService;

        public NotificationController(WebSocketConnectionManager connectionManager, ILogger<NotificationController> logger, VideoProcessingService videoService)
        {
            _connectionManager = connectionManager;
            _logger = logger;
            _videoService = videoService;
        }

        [HttpGet("ws")]
        public async Task ConnectToWebSocket(CancellationToken cancellationToken)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                _logger.LogWarning("Не WebSocket запрос.");
                HttpContext.Response.StatusCode = 400;
                return;
            }

            var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            var userId = HttpContext.Request.Query["userId"];
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Не указан userId для подключения.");
                await socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "UserId is required", cancellationToken);
                return;
            }

            await _connectionManager.Register(userId, socket);
            _logger.LogInformation("Пользователь подключился: {UserId}", userId);
        }

        [HttpPost("process-video")]
        public async Task<IActionResult> ProcessVideo([FromQuery] string userId)
        {
            await _videoService.ProcessVideoAndNotify(userId);
            return Ok("Процесс запущен");
        }
    }

}
