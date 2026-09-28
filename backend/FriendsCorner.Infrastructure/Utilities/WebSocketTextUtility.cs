using System.Net.WebSockets;
using System.Text;

namespace FriendsCorner.Infrastructure.Utilities;

public interface IWebSocketTextUtility
{
    Task<string?> Receive(WebSocket socket, byte[] buffer, CancellationToken cancellationToken);

    Task Send(WebSocket socket, string text, CancellationToken cancellationToken);
}

// Whole UTF-8 text messages over a WebSocket, which arrive in frames.
public sealed class WebSocketTextUtility : IWebSocketTextUtility
{
    public async Task<string?> Receive(WebSocket socket, byte[] buffer, CancellationToken cancellationToken)
    {
        using var message = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            message.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(message.ToArray());
    }

    // A browser that just left is not an error for the others.
    public async Task Send(WebSocket socket, string text, CancellationToken cancellationToken)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        try
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, cancellationToken);
        }
        catch (WebSocketException)
        {
        }
    }
}
