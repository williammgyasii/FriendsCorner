namespace FriendsCorner.Core.Engines.LetterTiles;

// A blank being played carries the letter it will stand for.
public sealed record PlacedTile(int Square, Tile Tile);

// Where tiles may go, checked in the order the refusals are listed in the spec.
// An empty play, an off-board square, or a square already taken cannot be
// part of a line, so they are all refused as not in line.
public static class Placement
{
    private const int Size = PremiumLayout.Size;

    public static RefusalReason? Check(IReadOnlyList<Tile?> board, IReadOnlyList<Tile> rack, IReadOnlyList<PlacedTile> placed)
    {
        if (placed.Count == 0)
        {
            return RefusalReason.NotInLine;
        }

        if (!Holds(rack, placed))
        {
            return RefusalReason.NotInRack;
        }

        var squares = placed.Select(tile => tile.Square).ToArray();
        if (squares.Any(square => square is < 0 or >= PremiumLayout.Squares || board[square] is not null)
            || squares.Distinct().Count() != squares.Length)
        {
            return RefusalReason.NotInLine;
        }

        var sameRow = squares.All(square => square / Size == squares[0] / Size);
        var sameColumn = squares.All(square => square % Size == squares[0] % Size);
        if (!sameRow && !sameColumn)
        {
            return RefusalReason.NotInLine;
        }

        var step = sameRow ? 1 : Size;
        for (var square = squares.Min(); square <= squares.Max(); square += step)
        {
            if (board[square] is null && !squares.Contains(square))
            {
                return RefusalReason.Gap;
            }
        }

        if (board.All(tile => tile is null))
        {
            if (!squares.Contains(PremiumLayout.Centre))
            {
                return RefusalReason.FirstMustCoverCentre;
            }

            return squares.Length < 2 ? RefusalReason.FirstNeedsTwoTiles : null;
        }

        return squares.Any(square => Neighbours(square).Any(next => board[next] is not null))
            ? null
            : RefusalReason.NotConnected;
    }

    public static IEnumerable<int> Neighbours(int square)
    {
        var (row, column) = (square / Size, square % Size);
        if (row > 0)
        {
            yield return square - Size;
        }

        if (row < Size - 1)
        {
            yield return square + Size;
        }

        if (column > 0)
        {
            yield return square - 1;
        }

        if (column < Size - 1)
        {
            yield return square + 1;
        }
    }

    public static bool Holds(IReadOnlyList<Tile> rack, IEnumerable<PlacedTile> placed) =>
        TryTake(rack, placed.Select(tile => tile.Tile.IsBlank ? Tile.Blank : tile.Tile), out _);

    // Removes each wanted tile from the rack once; false when one is missing.
    public static bool TryTake(IReadOnlyList<Tile> rack, IEnumerable<Tile> wanted, out List<Tile> left)
    {
        left = rack.ToList();
        foreach (var tile in wanted)
        {
            if (!left.Remove(tile))
            {
                return false;
            }
        }

        return true;
    }
}
