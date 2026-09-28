using FriendsCorner.Core.Accessors;

namespace FriendsCorner.Core.Engines.LetterTiles;

public sealed record FormedWord(IReadOnlyList<int> Squares, string Text);

// The main word runs the way the tiles were laid; every placed tile also
// makes a cross-word the other way. Only runs of 2 or more letters count.
// Expects a play that already passed Placement.
public static class WordFinder
{
    private const int Size = PremiumLayout.Size;

    public static IReadOnlyList<FormedWord> Find(IReadOnlyList<Tile?> board, IReadOnlyList<PlacedTile> placed)
    {
        var laid = board.ToArray();
        foreach (var tile in placed)
        {
            laid[tile.Square] = tile.Tile;
        }

        var down = placed.Count > 1 && placed[0].Square % Size == placed[1].Square % Size;
        var (along, across) = down ? (Size, 1) : (1, Size);

        return new[] { Run(laid, placed[0].Square, along) }
            .Concat(placed.Select(tile => Run(laid, tile.Square, across)))
            .Where(word => word.Squares.Count >= 2)
            .ToArray();
    }

    public static IReadOnlyList<string> Unknown(IEnumerable<FormedWord> words, IWordListAccessor wordList) =>
        words.Select(word => word.Text).Where(text => !wordList.Contains(text)).Distinct().ToArray();

    private static FormedWord Run(Tile?[] laid, int square, int step)
    {
        var start = square;
        while (Continues(start, -step) && laid[start - step] is not null)
        {
            start -= step;
        }

        var squares = new List<int> { start };
        while (Continues(squares[^1], step) && laid[squares[^1] + step] is not null)
        {
            squares.Add(squares[^1] + step);
        }

        return new FormedWord(squares, string.Concat(squares.Select(at => laid[at]!.Value.Letter)));
    }

    // A step of 1 must stay on the same row; a step of 15 must stay on the board.
    private static bool Continues(int square, int step)
    {
        var next = square + step;
        return next is >= 0 and < PremiumLayout.Squares && (Math.Abs(step) == Size || next / Size == square / Size);
    }
}
