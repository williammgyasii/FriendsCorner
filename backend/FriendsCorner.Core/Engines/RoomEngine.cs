namespace FriendsCorner.Core.Engines;

public enum Seat
{
    A,
    B,
    C,
    D,
}

public readonly record struct Position(double X, double Y);

public sealed class RoomEngine
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
        if (world is not ("room" or "tictactoe" or "chess"))
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

        if (world == "chess")
        {
            Chess ??= ChessBoard.Start();
        }

        return true;
    }

    public ChessBoard? Chess { get; private set; }

    public void RestoreChess(ChessBoard board)
    {
        World = "chess";
        Chess = board;
    }

    public bool TryChessMove(Seat seat, ChessMove move)
    {
        if (Chess is null || !Chess.TryMove(seat, move, out var updated))
        {
            return false;
        }

        Chess = updated;
        return true;
    }

    public bool TryChessRematch()
    {
        if (Chess is null || !Chess.TryRematch(out var updated))
        {
            return false;
        }

        Chess = updated;
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

    public LobbyEngine Lobby { get; } = new();

    public RoomChange Apply(Seat seat, RoomCommand command, DateTimeOffset now)
    {
        // Steering shows up on the next tick, so it never asks for a broadcast.
        if (command is Steer steer)
        {
            TrySetDirection(seat, steer.X, steer.Y);
            return RoomChange.None;
        }

        var changed = command switch
        {
            Place place => TryPlace(seat, place.Square),
            Rematch => TryRematch(),
            MoveChess move => TryChessMove(seat, move.Move),
            ChessRematch => TryChessRematch(),
            PickGame pick => Lobby.TryPick(seat, pick.Game),
            SetReady ready => Lobby.TrySetReady(seat, ready.Ready),
            SetCapacity capacity => Lobby.TrySetCapacity(seat, capacity.Size),
            ShareMedia media => ShareMedia(seat, media),
            StartGame => Lobby.TryStart(seat, now),
            _ => false,
        };

        return changed ? ChangeOf(command) : RoomChange.None;
    }

    // The face call is one-to-one between seats A and B for now.
    public static Seat? CallPartner(Seat seat) => seat switch
    {
        Seat.A => Seat.B,
        Seat.B => Seat.A,
        _ => null,
    };

    private bool ShareMedia(Seat seat, ShareMedia media)
    {
        Lobby.SetMedia(seat, media.Camera, media.Mic);
        return true;
    }

    private static RoomChange ChangeOf(RoomCommand command) => command switch
    {
        Place or Rematch => RoomChange.TicTacToe,
        MoveChess or ChessRematch => RoomChange.Chess,
        _ => RoomChange.Lobby,
    };

    public bool TryFinishCountdown(DateTimeOffset now) =>
        Lobby.TryFinishCountdown(now, out var world) && TryLaunch(world);

    // One step of the room's clock: players glide, and a finished countdown
    // opens the picked game, whose fresh board is then worth saving.
    public RoomChange Advance(double seconds, DateTimeOffset now)
    {
        Tick(seconds);
        if (!TryFinishCountdown(now))
        {
            return RoomChange.None;
        }

        return World switch
        {
            "tictactoe" => RoomChange.Lobby | RoomChange.TicTacToe,
            "chess" => RoomChange.Lobby | RoomChange.Chess,
            _ => RoomChange.Lobby,
        };
    }

    public bool TryAddSeat(out Seat seat)
    {
        seat = Enum.GetValues<Seat>().FirstOrDefault(free => !_positions.ContainsKey(free));
        if (_positions.ContainsKey(seat) || !Lobby.Join(seat))
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
        Lobby.Leave(seat);
    }
}
