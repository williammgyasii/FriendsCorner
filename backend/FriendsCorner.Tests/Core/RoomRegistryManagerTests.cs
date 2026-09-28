using System.Net.WebSockets;
using FriendsCorner.Core.Managers;
using FriendsCorner.Core.Accessors;

namespace FriendsCorner.Tests;

public class RoomRegistryManagerTests
{
    [Fact]
    public async Task A_saved_chess_game_comes_back_as_a_live_room()
    {
        Assert.True(ChessBoard.Start().TryMove(Seat.A, new ChessMove("e2", "e4"), out var saved));
        var factory = new FakeRoomFactory();
        var registry = new RoomRegistryManager(new FakeGameTable { Saved = new ChessGame(saved) }, factory);

        var room = await registry.Find("old-room");

        Assert.NotNull(room);
        Assert.True(registry.IsLive("old-room"));
        var restored = Assert.IsType<ChessGame>(Assert.Single(factory.Made).Restored);
        Assert.Same(saved, restored.Board);
    }

    [Fact]
    public async Task A_room_that_was_never_saved_is_not_found()
    {
        var registry = new RoomRegistryManager(new FakeGameTable(), new FakeRoomFactory());

        Assert.Null(await registry.Find("nowhere"));
        Assert.False(registry.IsLive("nowhere"));
    }

    [Fact]
    public async Task A_live_room_is_found_without_reading_the_database()
    {
        var games = new FakeGameTable();
        var registry = new RoomRegistryManager(games, new FakeRoomFactory());
        var id = registry.Create();

        Assert.NotNull(await registry.Find(id));
        Assert.Equal(0, games.Loads);
    }

    [Fact]
    public void An_empty_room_is_forgotten()
    {
        var factory = new FakeRoomFactory();
        var registry = new RoomRegistryManager(new FakeGameTable(), factory);
        var id = registry.Create();

        factory.Made[0].OnEmpty();

        Assert.False(registry.IsLive(id));
    }

    private sealed class FakeGameTable : IGameTableAccessor
    {
        public IGameEngine? Saved { get; init; }

        public int Loads { get; private set; }

        public Task<IGameEngine?> Load(string roomId)
        {
            Loads++;
            return Task.FromResult(Saved);
        }
    }

    private sealed class FakeRoomFactory : IRoomManagerFactory
    {
        public List<FakeRoom> Made { get; } = [];

        public IRoomManager Create(string roomId, Action onEmpty)
        {
            var room = new FakeRoom(onEmpty);
            Made.Add(room);
            return room;
        }
    }

    private sealed class FakeRoom(Action onEmpty) : IRoomManager
    {
        public Action OnEmpty { get; } = onEmpty;

        public IGameEngine? Restored { get; private set; }

        public void Restore(IGameEngine game) => Restored = game;

        public Task<Seat?> Join(WebSocket socket, CancellationToken cancellationToken) => Task.FromResult<Seat?>(null);

        public Task Act(Seat seat, RoomCommand command, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task Relay(Seat from, string json, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task Leave(Seat seat) => Task.CompletedTask;
    }
}
