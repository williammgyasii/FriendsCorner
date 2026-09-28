using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines.Games;
using FriendsCorner.Core.Engines.LetterTiles;

namespace FriendsCorner.Infrastructure.Accessors;

// Each game is saved in its own table, so this is the one place that knows
// which recorder a game goes to.
public sealed class GameRecorderAccessor(
    IBoardRecorderAccessor boards,
    IChessRecorderAccessor chess,
    ILetterTilesRecorderAccessor tiles) : IGameRecorderAccessor
{
    public void Note(string roomId, IGameEngine game)
    {
        switch (game)
        {
            case TicTacToeGame ticTacToe:
                boards.Note(roomId, ticTacToe.Board);
                break;
            case ChessGame chessGame:
                chess.Note(roomId, chessGame.Board);
                break;
            case LetterTilesGame letterTiles:
                tiles.Note(roomId, letterTiles.State);
                break;
        }
    }
}
