namespace FriendsCorner.Core.Engines.LetterTiles;

public enum Premium
{
    None,
    DoubleLetter,
    TripleLetter,
    DoubleWord,
    TripleWord,
}

// Square = row x 15 + column. The page is sent Text as-is, so this is the
// only place the premiums are decided.
public static class PremiumLayout
{
    public const int Size = 15;
    public const int Squares = Size * Size;
    public const int Centre = 112;

    public static string Text { get; } = string.Concat(
        "T..d...T...d..T",
        ".D...t...t...D.",
        "..D...d.d...D..",
        "d..D...d...D..d",
        "....D.....D....",
        ".t...t...t...t.",
        "..d...d.d...d..",
        "T..d...*...d..T",
        "..d...d.d...d..",
        ".t...t...t...t.",
        "....D.....D....",
        "d..D...d...D..d",
        "..D...d.d...D..",
        ".D...t...t...D.",
        "T..d...T...d..T");

    public static Premium At(int square) => Text[square] switch
    {
        'T' => Premium.TripleWord,
        'D' or '*' => Premium.DoubleWord,
        't' => Premium.TripleLetter,
        'd' => Premium.DoubleLetter,
        _ => Premium.None,
    };
}
