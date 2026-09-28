using FriendsCorner.Core.Engines.Games;

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

    private const string Floor = "room";

    private readonly GameCatalog _games;

    public RoomEngine()
        : this(new GameCatalog())
    {
    }

    public RoomEngine(GameCatalog games) => _games = games;

    public string? World { get; private set; }

    public IGameEngine? Game { get; private set; }

    public bool TryLaunch(string world)
    {
        if (World is not null)
        {
            return World == world;
        }

        if (world != Floor)
        {
            if (!_games.TryStart(world, Lobby.Playing, out var game))
            {
                return false;
            }

            Game = game;
        }

        World = world;
        return true;
    }

    public void Restore(IGameEngine game)
    {
        World = game.Id;
        Game = game;
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
            GameMove move => Game?.TryPlay(seat, move) ?? false,
            Rematch => Game?.TryRematch() ?? false,
            PickGame pick => Lobby.TryPick(seat, pick.Game),
            SetReady ready => Lobby.TrySetReady(seat, ready.Ready),
            SetCapacity capacity => Lobby.TrySetCapacity(seat, capacity.Size),
            ShareMedia media => ShareMedia(seat, media),
            StartGame => Lobby.TryStart(seat, now),
            _ => false,
        };

        return changed ? ChangeOf(command) : RoomChange.None;
    }

    public GameAnswer? Ask(Seat seat, GameQuestion question) => Game?.Ask(seat, question);

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

    private static RoomChange ChangeOf(RoomCommand command) =>
        command is GameMove or Rematch ? RoomChange.Game : RoomChange.Lobby;

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

        return Game is null ? RoomChange.Lobby : RoomChange.Lobby | RoomChange.Game;
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
