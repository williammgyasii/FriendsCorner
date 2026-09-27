namespace DistanceTogether;

public sealed class Board
{
    private static readonly int[][] Lines =
    [
        [0, 1, 2],
        [3, 4, 5],
        [6, 7, 8],
        [0, 3, 6],
        [1, 4, 7],
        [2, 5, 8],
        [0, 4, 8],
        [2, 4, 6],
    ];

    private Board(char?[] squares, Seat next, Seat? winner, bool isDraw)
    {
        Squares = squares;
        Next = next;
        Winner = winner;
        IsDraw = isDraw;
    }

    public IReadOnlyList<char?> Squares { get; }

    public Seat Next { get; }

    public Seat? Winner { get; }

    public bool IsDraw { get; }

    public static Board Empty() => new(new char?[9], Seat.A, null, false);

    public bool TryPlace(Seat seat, int square, out Board updated)
    {
        updated = this;
        if (Winner is not null || IsDraw || seat != Next || square is < 0 or > 8 || Squares[square] is not null)
        {
            return false;
        }

        var cells = Squares.ToArray();
        cells[square] = seat == Seat.A ? 'X' : 'O';
        var winner = WinnerOf(cells);
        var draw = winner is null && cells.All(cell => cell is not null);
        var next = seat == Seat.A ? Seat.B : Seat.A;
        updated = new Board(cells, next, winner, draw);
        return true;
    }

    public bool TryRematch(out Board updated)
    {
        updated = this;
        if (Winner is null && !IsDraw)
        {
            return false;
        }

        updated = Empty();
        return true;
    }

    public static Board FromRow(string squares, string nextSeat)
    {
        if (squares.Length != 9)
        {
            throw new ArgumentException("A board row has nine squares.", nameof(squares));
        }

        var cells = new char?[9];
        for (var i = 0; i < 9; i++)
        {
            cells[i] = squares[i] == ' ' ? null : squares[i];
        }

        var next = nextSeat == nameof(Seat.B) ? Seat.B : Seat.A;
        var winner = WinnerOf(cells);
        var draw = winner is null && cells.All(cell => cell is not null);
        return new Board(cells, next, winner, draw);
    }

    public string SquaresRow()
    {
        var row = new char[9];
        for (var i = 0; i < 9; i++)
        {
            row[i] = Squares[i] ?? ' ';
        }

        return new string(row);
    }

    private static Seat? WinnerOf(char?[] cells)
    {
        foreach (var line in Lines)
        {
            var mark = cells[line[0]];
            if (mark is not null && mark == cells[line[1]] && mark == cells[line[2]])
            {
                return mark == 'X' ? Seat.A : Seat.B;
            }
        }

        return null;
    }
}
