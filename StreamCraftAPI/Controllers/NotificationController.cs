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

        public NotificationController(WebSocketConnectionManager connectionManager, ILogger<NotificationController> logger)
        {
            _connectionManager = connectionManager;
            _logger = logger;
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

            var userId = HttpContext.Request.Query["userId"].ToString();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Не указан userId для подключения.");
                await socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "UserId is required", cancellationToken);
                return;
            }

            await _connectionManager.Register(userId, socket);
            _logger.LogInformation("Пользователь подключился: {UserId}", userId);

            var buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _connectionManager.Unregister(userId);
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", cancellationToken);
                }
            }
        }
    }

}
