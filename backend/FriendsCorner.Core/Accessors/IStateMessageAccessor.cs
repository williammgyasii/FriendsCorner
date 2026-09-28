using FriendsCorner.Core.Engines;

namespace FriendsCorner.Core.Accessors;

// What each browser is sent, in the wire format the page reads.
public interface IStateMessageAccessor
{
    string Joined(Seat seat);

    string Write(RoomEngine room, Seat you, DateTimeOffset now);
}
