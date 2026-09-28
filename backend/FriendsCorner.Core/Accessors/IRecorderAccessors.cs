using FriendsCorner.Core.Engines;
using FriendsCorner.Core.Engines.Games;
using FriendsCorner.Core.Engines.LetterTiles;

namespace FriendsCorner.Core.Accessors;

// Takes whatever game a room is running and keeps a copy of it to save.
// Call it while the game cannot change, so the copy is the one just made.
public interface IGameRecorderAccessor
{
    void Note(string roomId, IGameEngine game);
}

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

public interface ILetterTilesRecorderAccessor : ISnapshotRecorderAccessor<LetterTilesState>;
