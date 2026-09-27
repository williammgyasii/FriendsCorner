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

    private readonly Dictionary<Seat, Position> _positions = new();
    private readonly Dictionary<Seat, (double X, double Y)> _directions = new();

    public IReadOnlyDictionary<Seat, Position> Positions => _positions;

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

        _directions[seat] = (x, y);
        return true;
    }

    public void Tick(double seconds)
    {
        foreach (var seat in _positions.Keys.ToList())
        {
            var position = _positions[seat];
            var direction = _directions[seat];
            _positions[seat] = new Position(
                position.X + (direction.X * Speed * seconds),
                position.Y + (direction.Y * Speed * seconds));
        }
    }

    public void Free(Seat seat)
    {
        _positions.Remove(seat);
        _directions.Remove(seat);
    }
}
