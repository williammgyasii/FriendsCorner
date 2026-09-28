namespace FriendsCorner.Core.Engines.Games;

public sealed class TicTacToeGame(Board board) : IGameEngine
{
    public const string GameId = "tictactoe";

    public string Id => GameId;

    public Board Board { get; private set; } = board;

    public bool TryPlay(Seat seat, GameMove move)
    {
        if (move is not Place place || !Board.TryPlace(seat, place.Square, out var updated))
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
