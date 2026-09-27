namespace DistanceTogether;

public enum Seat
{
    A,
    B,
}

public readonly record struct Position(double X, double Y);

public sealed class Room
{
    private const double Speed = 160;
    private const double FloorWidth = 480;
    private const double FloorHeight = 320;

    private readonly Dictionary<Seat, Position> _positions = new();
    private readonly Dictionary<Seat, (double X, double Y)> _directions = new();

    public IReadOnlyDictionary<Seat, Position> Positions => _positions;

    public string? World { get; private set; }

    public bool TryLaunch(string world)
    {
        if (world is not ("room" or "tictactoe"))
        {
            return false;
        }

        if (World is not null && World != world)
        {
            return false;
        }

        World = world;
        if (world == "tictactoe")
        {
            TicTacToe ??= Board.Empty();
        }

        return true;
    }

    public Board? TicTacToe { get; private set; }

    public void RestoreTicTacToe(Board board)
    {
        World = "tictactoe";
        TicTacToe = board;
    }

    public bool TryPlace(Seat seat, int square)
    {
        if (TicTacToe is null || !TicTacToe.TryPlace(seat, square, out var updated))
        {
            return false;
        }

        TicTacToe = updated;
        return true;
    }

    public bool TryRematch()
    {
        if (TicTacToe is null || !TicTacToe.TryRematch(out var updated))
        {
            return false;
        }

        TicTacToe = updated;
        return true;
    }

    public bool TryAddSeat(out Seat seat)
    {
        if (!_positions.ContainsKey(Seat.A))
        {
            seat = Seat.A;
        }
        else if (!_positions.ContainsKey(Seat.B))
        {
            seat = Seat.B;
        }
        else
        {
            seat = default;
            return false;
        }

        _positions[seat] = new Position(240, 160);
        _directions[seat] = (0, 0);
        return true;
    }

    public bool TrySetDirection(Seat seat, double x, double y)
    {
        if (!_directions.ContainsKey(seat))
        {
            return false;
        }

        if (x is < -1 or > 1 || y is < -1 or > 1)
        {
            return false;
        }

        _directions[seat] = (x, y);
        return true;
    }

    public void Tick(double seconds)
    {
        foreach (var seat in _positions.Keys.ToList())
        {
            var position = _positions[seat];
            var direction = _directions[seat];
            var length = Math.Sqrt((direction.X * direction.X) + (direction.Y * direction.Y));
            var scale = length > 1 ? 1 / length : 1;
            var x = position.X + (direction.X * scale * Speed * seconds);
            var y = position.Y + (direction.Y * scale * Speed * seconds);
            _positions[seat] = new Position(
                Math.Clamp(x, 0, FloorWidth),
                Math.Clamp(y, 0, FloorHeight));
        }
    }

    public void Free(Seat seat)
    {
        _positions.Remove(seat);
        _directions.Remove(seat);
    }
}
