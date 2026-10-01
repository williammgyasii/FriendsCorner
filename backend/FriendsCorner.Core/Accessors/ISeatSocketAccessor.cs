using System.Net.WebSockets;
using FriendsCorner.Core.Engines;

namespace FriendsCorner.Core.Accessors;

// The open browser connections in one room, by seat.
public interface ISeatSocketAccessor
{
    IReadOnlyList<Seat> Seats { get; }

    void Add(Seat seat, WebSocket socket);

    bool Holds(Seat seat, WebSocket socket);

    // The socket this seat had before, when a new one takes its place.
    WebSocket? Replace(Seat seat, WebSocket socket);

    // True when that was the last connection.
    bool Remove(Seat seat);

    Task Send(Seat seat, string json, CancellationToken cancellationToken);

    Task SendEach(IEnumerable<(Seat Seat, string Json)> messages, CancellationToken cancellationToken);
}
