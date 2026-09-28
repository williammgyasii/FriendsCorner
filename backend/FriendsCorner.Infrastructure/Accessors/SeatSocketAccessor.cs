using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;
using System.Net.WebSockets;
using FriendsCorner.Infrastructure.Utilities;

namespace FriendsCorner.Infrastructure.Accessors;

// The open browser connections in one room, by seat. One per room.
public sealed class SeatSocketAccessor : ISeatSocketAccessor
{
    private readonly Dictionary<Seat, WebSocket> _sockets = new();
    private readonly Lock _gate = new();
    private readonly IWebSocketTextUtility _text;

    public SeatSocketAccessor(IWebSocketTextUtility text) => _text = text;

    public IReadOnlyList<Seat> Seats
    {
        get
        {
            lock (_gate)
            {
                return _sockets.Keys.ToList();
            }
        }
    }

    public void Add(Seat seat, WebSocket socket)
    {
        lock (_gate)
        {
            _sockets[seat] = socket;
        }
    }

    // True when that was the last connection.
    public bool Remove(Seat seat)
    {
        lock (_gate)
        {
            _sockets.Remove(seat);
            return _sockets.Count == 0;
        }
    }

    public Task Send(Seat seat, string json, CancellationToken cancellationToken)
    {
        WebSocket? socket;
        lock (_gate)
        {
            _sockets.TryGetValue(seat, out socket);
        }

        return socket is null ? Task.CompletedTask : _text.Send(socket, json, cancellationToken);
    }

    public async Task SendEach(IEnumerable<(Seat Seat, string Json)> messages, CancellationToken cancellationToken)
    {
        foreach (var (seat, json) in messages)
        {
            await Send(seat, json, cancellationToken);
        }
    }
}
