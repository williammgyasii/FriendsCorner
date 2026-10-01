using FriendsCorner.Core.Accessors;

namespace FriendsCorner.Core.Managers;

public interface IRoomRegistryManager
{
    string Create(Guid hostUserId);

    bool IsLive(string id);

    Task<IRoomManager?> Find(string id);
}

// Opens rooms: a new one, a live one, or a saved game brought back to life.
public sealed class RoomRegistryManager : IRoomRegistryManager
{
    private readonly Dictionary<string, IRoomManager> _rooms = new();
    private readonly Lock _gate = new();
    private readonly IGameTableAccessor _games;
    private readonly IRoomManagerFactory _factory;

    public RoomRegistryManager(IGameTableAccessor games, IRoomManagerFactory factory)
    {
        _games = games;
        _factory = factory;
    }

    public string Create(Guid hostUserId)
    {
        var id = Guid.NewGuid().ToString("N");
        lock (_gate)
        {
            var room = NewRoom(id);
            room.RememberHost(hostUserId);
            _rooms[id] = room;
        }

        return id;
    }

    public bool IsLive(string id)
    {
        lock (_gate)
        {
            return _rooms.ContainsKey(id);
        }
    }

    public async Task<IRoomManager?> Find(string id)
    {
        lock (_gate)
        {
            if (_rooms.TryGetValue(id, out var live))
            {
                return live;
            }
        }

        if (await _games.Load(id) is not { } game)
        {
            return null;
        }

        lock (_gate)
        {
            if (_rooms.TryGetValue(id, out var live))
            {
                return live;
            }

            var room = NewRoom(id);
            room.Restore(game);

            _rooms[id] = room;
            return room;
        }
    }

    private IRoomManager NewRoom(string id) => _factory.Create(id, () => Close(id));

    private void Close(string id)
    {
        lock (_gate)
        {
            _rooms.Remove(id);
        }
    }
}
