using System.Net.WebSockets;
using FriendsCorner.Core.Managers;
using FriendsCorner.Core.Accessors;
using FriendsCorner.Infrastructure.Accessors;

namespace FriendsCorner.Tests;

public class RoomRegistryManagerTests
{
    [Fact]
    public async Task A_saved_chess_game_comes_back_as_a_live_room()
    {
        Assert.True(ChessBoard.Start().TryMove(Seat.A, new ChessMove("e2", "e4"), out var saved));
        var factory = new FakeRoomFactory();
        var registry = new RoomRegistryManager(new FakeBoardTable(), new FakeChessTable { Saved = saved }, factory);

        var room = await registry.Find("old-room");

        Assert.NotNull(room);
        Assert.True(registry.IsLive("old-room"));
        Assert.Same(saved, Assert.Single(factory.Made).RestoredChess);
    }

    [Fact]
    public async Task A_room_that_was_never_saved_is_not_found()
    {
        var registry = new RoomRegistryManager(new FakeBoardTable(), new FakeChessTable(), new FakeRoomFactory());

        Assert.Null(await registry.Find("nowhere"));
        Assert.False(registry.IsLive("nowhere"));
    }

    [Fact]
    public async Task A_live_room_is_found_without_reading_the_database()
    {
        var chess = new FakeChessTable();
        var registry = new RoomRegistryManager(new FakeBoardTable(), chess, new FakeRoomFactory());
        var id = registry.Create();

        Assert.NotNull(await registry.Find(id));
        Assert.Equal(0, chess.Loads);
    }

    [Fact]
    public void An_empty_room_is_forgotten()
    {
        var factory = new FakeRoomFactory();
        var registry = new RoomRegistryManager(new FakeBoardTable(), new FakeChessTable(), factory);
        var id = registry.Create();

        factory.Made[0].OnEmpty();

        Assert.False(registry.IsLive(id));
    }

    private sealed class FakeBoardTable : IBoardTableAccessor
    {
        public Task Save(string roomId, Board board) => Task.CompletedTask;

        public Task<Board?> Load(string roomId) => Task.FromResult<Board?>(null);

        public Task Remove(string roomId) => Task.CompletedTask;
    }

    private sealed class FakeChessTable : IChessTableAccessor
    {
        public ChessBoard? Saved { get; init; }

        public int Loads { get; private set; }

        public Task Save(string roomId, ChessBoard board) => Task.CompletedTask;

        public Task<ChessBoard?> Load(string roomId)
        {
            Loads++;
            return Task.FromResult(Saved);
        }

        public Task Remove(string roomId) => Task.CompletedTask;
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

        public ChessBoard? RestoredChess { get; private set; }

        public void RestoreTicTacToe(Board board)
        {
        }

        public void RestoreChess(ChessBoard board) => RestoredChess = board;

        public Task<Seat?> Join(WebSocket socket, CancellationToken cancellationToken) => Task.FromResult<Seat?>(null);

        public Task Act(Seat seat, RoomCommand command, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task Relay(Seat from, string json, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task Leave(Seat seat) => Task.CompletedTask;
    }
}
