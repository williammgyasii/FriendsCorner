using FriendsCorner.Core.Engines;
using FriendsCorner.Core.Engines.Games;

namespace FriendsCorner.Core.Accessors;

// What each browser is sent, in the wire format the page reads.
public interface IStateMessageAccessor
{
    string Joined(Seat seat);

    string Write(RoomEngine room, Seat you, DateTimeOffset now);

    // Null for an answer the page has no message for.
    string? Answer(GameAnswer answer);
}
