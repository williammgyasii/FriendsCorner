namespace FriendsCorner.Core.Engines;

public readonly record struct Media(bool Camera, bool Mic);

public readonly record struct Game(string Id, int Players);

// Who is in, who runs it, what they will play, and when it starts. The server
// owns this so every player sees the same game master, pick, and countdown.
public sealed class LobbyEngine
{
    public const int MinCapacity = 2;

    // The face call connects every browser to every other one; past four
    // that gets heavy without a media server.
    public const int MaxCapacity = 4;

    public static readonly TimeSpan Countdown = TimeSpan.FromSeconds(3);

    public static readonly IReadOnlyList<Game> Games =
    [
        new("room", MaxCapacity),
        new("tictactoe", 2),
        new("chess", 2),
        new("tiles", MaxCapacity),
    ];

    private readonly List<Seat> _players = [];
    private readonly HashSet<Seat> _ready = [];
    private readonly Dictionary<Seat, Media> _media = new();

    public IReadOnlyList<Seat> Players => _players;

    public Seat? Host => _players.Count > 0 ? _players[0] : null;

    public int Capacity { get; private set; } = MinCapacity;

    public string? Pick { get; private set; }

    public DateTimeOffset? CountdownEndsAt { get; private set; }

    public bool IsFull => _players.Count >= Capacity;

    public IReadOnlyList<Seat> Playing => _players.Take(PlayerCount).ToList();

    public IReadOnlyList<Seat> Watching => _players.Skip(PlayerCount).ToList();

    public bool CanStart =>
        Pick is not null &&
        _players.Count == Capacity &&
        _players.Skip(1).All(_ready.Contains);

    private int PlayerCount
    {
        get
        {
            var game = Games.FirstOrDefault(known => known.Id == Pick);
            return game.Players > 0 ? game.Players : _players.Count;
        }
    }

    public bool Join(Seat seat)
    {
        if (IsFull || _players.Contains(seat))
        {
            return false;
        }

        _players.Add(seat);
        _media[seat] = new Media(Camera: true, Mic: true);
        return true;
    }

    public void Leave(Seat seat)
    {
        if (!_players.Remove(seat))
        {
            return;
        }

        _ready.Remove(seat);
        _media.Remove(seat);
        CountdownEndsAt = null;
    }

    public bool TryPick(Seat by, string game)
    {
        if (by != Host || CountdownEndsAt is not null || Games.All(known => known.Id != game))
        {
            return false;
        }

        if (Pick != game)
        {
            _ready.Clear();
        }

        Pick = game;
        return true;
    }

    public bool TrySetCapacity(Seat by, int capacity)
    {
        if (by != Host || CountdownEndsAt is not null ||
            capacity is < MinCapacity or > MaxCapacity || capacity < _players.Count)
        {
            return false;
        }

        Capacity = capacity;
        return true;
    }

    public bool TrySetReady(Seat seat, bool ready)
    {
        if (!_players.Contains(seat) || seat == Host || CountdownEndsAt is not null)
        {
            return false;
        }

        if (ready)
        {
            _ready.Add(seat);
        }
        else
        {
            _ready.Remove(seat);
        }

        return true;
    }

    public bool IsReady(Seat seat) => _ready.Contains(seat);

    public void SetMedia(Seat seat, bool camera, bool mic)
    {
        if (_players.Contains(seat))
        {
            _media[seat] = new Media(camera, mic);
        }
    }

    public Media MediaOf(Seat seat) => _media.GetValueOrDefault(seat);

    public bool TryStart(Seat by, DateTimeOffset now)
    {
        if (by != Host || !CanStart || CountdownEndsAt is not null)
        {
            return false;
        }

        CountdownEndsAt = now + Countdown;
        return true;
    }

    public bool TryFinishCountdown(DateTimeOffset now, out string world)
    {
        world = Pick ?? "";
        if (CountdownEndsAt is not { } endsAt || now < endsAt)
        {
            return false;
        }

        CountdownEndsAt = null;
        _ready.Clear();
        return true;
    }
}
