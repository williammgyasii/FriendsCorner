namespace FriendsCorner.Core.Engines.LetterTiles;

// A blank on a rack is '?'; once played it carries the letter it stands for
// and stays worth nothing.
public readonly record struct Tile(char Letter, bool IsBlank = false) : IComparable<Tile>
{
    public const char Unassigned = '?';

    public static Tile Blank => new(Unassigned, IsBlank: true);

    public Tile As(char letter) => this with { Letter = char.ToUpperInvariant(letter) };

    public int CompareTo(Tile other) =>
        IsBlank != other.IsBlank ? IsBlank.CompareTo(other.IsBlank) : Letter.CompareTo(other.Letter);
}
