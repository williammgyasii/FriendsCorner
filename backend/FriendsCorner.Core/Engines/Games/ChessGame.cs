namespace FriendsCorner.Core.Engines.Games;

public sealed class ChessGame(ChessBoard board) : IGameEngine
{
    public const string GameId = "chess";

    public string Id => GameId;

    public ChessBoard Board { get; private set; } = board;

    public bool TryPlay(Seat seat, GameMove move)
    {
        if (move is not MoveChess chess || !Board.TryMove(seat, chess.Move, out var updated))
        {
            return false;
        }

        Board = updated;
        return true;
    }

    public bool TryRematch()
    {
        if (!Board.TryRematch(out var updated))
        {
            return false;
        }

        Board = updated;
        return true;
    }
}
