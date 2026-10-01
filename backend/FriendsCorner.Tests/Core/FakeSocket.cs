using System.Net.WebSockets;
using System.Text;

namespace FriendsCorner.Tests;

internal sealed class FakeSocket : WebSocket
{
    public List<string> Sent { get; } = [];

    public override WebSocketCloseStatus? CloseStatus => null;

    public override string? CloseStatusDescription => null;

    public override WebSocketState State => WebSocketState.Open;

    public override string? SubProtocol => null;

    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        lock (Sent)
        {
            Sent.Add(Encoding.UTF8.GetString(buffer));
        }

        return Task.CompletedTask;
    }

    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public override void Abort()
    {
    }

    public override void Dispose()
    {
    }
}
