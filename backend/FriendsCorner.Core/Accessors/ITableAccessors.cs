using FriendsCorner.Core.Engines;
using FriendsCorner.Core.Engines.Games;
using FriendsCorner.Core.Engines.LetterTiles;
using FriendsCorner.Core.Engines.Mystery;

namespace FriendsCorner.Core.Accessors;

// Finds the saved game for a room, whichever game it was.
public interface IGameTableAccessor
{
    Task<IGameEngine?> Load(string roomId);
}

public interface IBoardTableAccessor
{
    Task Save(string roomId, Board board);

    Task<Board?> Load(string roomId);

    Task Remove(string roomId);
}

public interface IChessTableAccessor
{
    Task Save(string roomId, ChessBoard board);

    Task<ChessBoard?> Load(string roomId);

    Task Remove(string roomId);
}

public interface ILetterTilesTableAccessor
{
    Task Save(string roomId, LetterTilesState state);

    Task<LetterTilesState?> Load(string roomId);

    Task Remove(string roomId);
}

public interface IMysteryTableAccessor
{
    Task Save(string roomId, MysteryState state);

    Task<MysteryState?> Load(string roomId);

    Task Remove(string roomId);
}
