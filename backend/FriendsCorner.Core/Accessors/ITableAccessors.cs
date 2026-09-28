using FriendsCorner.Core.Engines;

namespace FriendsCorner.Core.Accessors;

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
