using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines.Games;
using FriendsCorner.Core.Engines.LetterTiles;
using FriendsCorner.Core.Engines.Mystery;

namespace FriendsCorner.Infrastructure.Accessors;

// A room runs one game, so at most one table has a row for it. A restored
// Letter Tiles game gets fresh randomness: the bag order is already saved.
public sealed class GameTableAccessor(
    IBoardTableAccessor boards,
    IChessTableAccessor chess,
    ILetterTilesTableAccessor tiles,
    IMysteryTableAccessor mystery,
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

        if (await tiles.Load(roomId) is { } state)
        {
            return new LetterTilesGame(state, words, new Random());
        }

        return await mystery.Load(roomId) is { } saved ? MysteryGame.Restore(saved) : null;
    }
}
