namespace FriendsCorner.Core.Engines.LetterTiles;

public static class TileSet
{
    private static readonly (char Letter, int Count, int Value)[] Letters =
    [
        ('A', 9, 1), ('B', 2, 3), ('C', 2, 3), ('D', 4, 2), ('E', 12, 1), ('F', 2, 4), ('G', 3, 2),
        ('H', 2, 4), ('I', 9, 1), ('J', 1, 8), ('K', 1, 5), ('L', 4, 1), ('M', 2, 3), ('N', 6, 1),
        ('O', 8, 1), ('P', 2, 3), ('Q', 1, 10), ('R', 6, 1), ('S', 4, 1), ('T', 6, 1), ('U', 4, 1),
        ('V', 2, 4), ('W', 2, 4), ('X', 1, 8), ('Y', 2, 4), ('Z', 1, 10),
    ];

    private const int Blanks = 2;

    private static readonly Dictionary<char, int> Values = Letters.ToDictionary(entry => entry.Letter, entry => entry.Value);

    public static int ValueOf(Tile tile) => tile.IsBlank ? 0 : Values.GetValueOrDefault(tile.Letter);

    public static IReadOnlyList<Tile> FullBag() =>
        Letters
            .SelectMany(entry => Enumerable.Repeat(new Tile(entry.Letter), entry.Count))
            .Concat(Enumerable.Repeat(Tile.Blank, Blanks))
            .ToArray();

    public static IReadOnlyList<Tile> ShuffledBag(Random random) => Shuffled(FullBag(), random);

    public static IReadOnlyList<Tile> Shuffled(IEnumerable<Tile> tiles, Random random)
    {
        var shuffled = tiles.ToArray();
        random.Shuffle(shuffled);
        return shuffled;
    }
}
