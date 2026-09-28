using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines.LetterTiles;

namespace FriendsCorner.Core.Engines.Games;

// Which games a room can launch, and how each one starts. Without a word
// list the catalog has no Letter Tiles.
public sealed class GameCatalog
{
    private readonly Dictionary<string, Func<IReadOnlyList<Seat>, IGameEngine?>> _starts = new()
    {
        [TicTacToeGame.GameId] = _ => new TicTacToeGame(Board.Empty()),
        [ChessGame.GameId] = _ => new ChessGame(ChessBoard.Start()),
    };

    public GameCatalog()
    {
    }

    public GameCatalog(IWordListAccessor words, Func<Random> random)
    {
        _starts[LetterTilesGame.GameId] = playing =>
            playing.Count < LetterTilesGame.MinPlayers ? null : LetterTilesGame.Start(playing, words, random());
    }

    public bool TryStart(string id, IReadOnlyList<Seat> playing, out IGameEngine game)
    {
        game = null!;
        if (!_starts.TryGetValue(id, out var start) || start(playing) is not { } started)
        {
            return false;
        }

        game = started;
        return true;
    }
}
