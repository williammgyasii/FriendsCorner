namespace FriendsCorner.Core.Engines.LetterTiles;

// Premiums count only under tiles laid this turn.
public static class Scorer
{
    public const int FullRack = 7;
    public const int FullRackBonus = 50;

    public static int Score(IReadOnlyList<Tile?> board, IReadOnlyList<PlacedTile> placed, IEnumerable<FormedWord> words)
    {
        var fresh = placed.ToDictionary(tile => tile.Square, tile => tile.Tile);
        var total = words.Sum(word => WordScore(board, fresh, word));
        return placed.Count == FullRack ? total + FullRackBonus : total;
    }

    private static int WordScore(IReadOnlyList<Tile?> board, Dictionary<int, Tile> fresh, FormedWord word)
    {
        var letters = 0;
        var multiplier = 1;
        foreach (var square in word.Squares)
        {
            if (!fresh.TryGetValue(square, out var tile))
            {
                letters += TileSet.ValueOf(board[square]!.Value);
                continue;
            }

            var premium = PremiumLayout.At(square);
            letters += TileSet.ValueOf(tile) * premium switch
            {
                Premium.DoubleLetter => 2,
                Premium.TripleLetter => 3,
                _ => 1,
            };
            multiplier *= premium switch
            {
                Premium.DoubleWord => 2,
                Premium.TripleWord => 3,
                _ => 1,
            };
        }

        return letters * multiplier;
    }
}
