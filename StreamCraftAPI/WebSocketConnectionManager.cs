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
            _logger.LogInformation("WebSocket зарегистрирован: {UserId}", userId);
            await Listen(userId, socket);
        }

        public async Task Unregister(string userId)
        {
            if (_sockets.TryRemove(userId, out var socket))
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
            }
        }

        public async Task SendToUser(string userId, string message)
        {
            if (_sockets.TryGetValue(userId, out var socket))
            {
                if (socket.State == WebSocketState.Open)
                {
                    var buffer = Encoding.UTF8.GetBytes(message);
                    await socket.SendAsync(
                        new ArraySegment<byte>(buffer),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                    _logger?.LogInformation("Сообщение отправлено пользователю {UserId}: {Message}", userId, message);
                }
                else
                {
                    _logger?.LogWarning("WebSocket для пользователя {UserId} не открыт. State: {State}", userId, socket.State);
                }
            }
            else
            {
                _logger?.LogWarning("WebSocket не найден для пользователя {UserId}", userId);
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
