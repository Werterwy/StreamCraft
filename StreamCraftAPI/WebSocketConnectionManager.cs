using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace StreamCraftAPI
{
    public class WebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
        private ILogger? _logger;

        public WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger)
        {
            _logger = logger;
        }

        public async Task Register(string userId, WebSocket socket)
        {
            _sockets[userId] = socket;
            await Listen(userId, socket);
        }

        public async Task SendToUser(string userId, string message)
        {
            if (_sockets.TryGetValue(userId, out var socket) && socket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
        }

        private async Task Listen(string userId, WebSocket socket)
        {
            var buffer = new byte[1024 * 4];
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (msg == "ping")
                    {
                        _logger?.LogDebug($"Ping получен от пользователя {userId}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Ошибка WebSocket у пользователя {UserId}", userId);
            }
            finally
            {
                _sockets.TryRemove(userId, out _);
                _logger?.LogInformation("WebSocket соединение закрыто: {UserId}", userId);

                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Ошибка при закрытии WebSocket для {UserId}", userId);
                }
            }
        }
    }


}
