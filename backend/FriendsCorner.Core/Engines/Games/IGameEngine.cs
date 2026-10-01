namespace FriendsCorner.Core.Engines.Games;

// A move meant for whichever game the room is running.
public abstract record GameMove : RoomCommand;

// Asks the running game something without changing it. The answer goes to the
// asking seat only; nothing is saved or broadcast.
public abstract record GameQuestion : RoomCommand;

public abstract record GameAnswer;

// One game's rules behind the one door the room knows about. How a game is
// shown or saved belongs to the layers outside Core.
public interface IGameEngine
{
    string Id { get; }

    bool TryPlay(Seat seat, GameMove move);

    bool TryRematch();

    GameAnswer? Ask(Seat seat, GameQuestion question) => null;

    // The room calls this every tick. Moves never see the time; a game with
    // a clock starts and runs it here, and says whether anything changed.
    bool TryAdvance(DateTimeOffset now) => false;
}
