using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines.Games;
using FriendsCorner.Core.Engines.LetterTiles;

namespace FriendsCorner.Infrastructure.Accessors;

// A room runs one game, so at most one table has a row for it. A restored
// Letter Tiles game gets fresh randomness: the bag order is already saved.
public sealed class GameTableAccessor(
    IBoardTableAccessor boards,
    IChessTableAccessor chess,
    ILetterTilesTableAccessor tiles,
    IWordListAccessor words) : IGameTableAccessor
{
    public async Task<IGameEngine?> Load(string roomId)
    {
        if (await boards.Load(roomId) is { } board)
        {
            return new TicTacToeGame(board);
        }

        if (await chess.Load(roomId) is { } chessBoard)
        {
            return new ChessGame(chessBoard);
        }

        return await tiles.Load(roomId) is { } state ? new LetterTilesGame(state, words, new Random()) : null;
    }
}
