using FriendsCorner.Core.Engines;

namespace FriendsCorner.Core.Accessors;

// Takes a board to save later, so a move never waits on the database.
public interface ISnapshotRecorderAccessor<TBoard>
    where TBoard : class
{
    void Note(string roomId, TBoard board);

    IReadOnlyList<(string RoomId, TBoard Board)> TakeWaiting();

    Task WhenQuiet();
}

public interface IBoardRecorderAccessor : ISnapshotRecorderAccessor<Board>;

public interface IChessRecorderAccessor : ISnapshotRecorderAccessor<ChessBoard>;
