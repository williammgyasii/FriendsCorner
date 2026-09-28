using FriendsCorner.Core.Accessors;
using FriendsCorner.Infrastructure.Accessors;

namespace FriendsCorner.Tests;

public class BoardTableAccessorTests
{
    [Fact]
    public async Task An_empty_board_is_nine_empty_squares_when_read_back()
    {
        var table = new BoardTableAccessor(await TestDatabase.Contexts());
        var roomId = Guid.NewGuid().ToString("N");

        await table.Save(roomId, Board.Empty());
        var loaded = await table.Load(roomId);

        Assert.NotNull(loaded);
        Assert.Equal(9, loaded.Squares.Count);
        Assert.All(loaded.Squares, square => Assert.Null(square));
        Assert.Equal(Seat.A, loaded.Next);

        await table.Remove(roomId);
    }

    [Fact]
    public async Task A_move_is_still_there_when_the_board_is_read_back()
    {
        var table = new BoardTableAccessor(await TestDatabase.Contexts());
        var roomId = Guid.NewGuid().ToString("N");
        Assert.True(Board.Empty().TryPlace(Seat.A, 4, out var played));

        await table.Save(roomId, played);
        var loaded = await table.Load(roomId);

        Assert.NotNull(loaded);
        Assert.Equal('X', loaded.Squares[4]);
        Assert.Equal(Seat.B, loaded.Next);

        await table.Remove(roomId);
    }

    [Fact]
    public async Task Saving_the_same_room_twice_keeps_only_the_latest_board()
    {
        var table = new BoardTableAccessor(await TestDatabase.Contexts());
        var roomId = Guid.NewGuid().ToString("N");
        Assert.True(Board.Empty().TryPlace(Seat.A, 0, out var first));
        Assert.True(first.TryPlace(Seat.B, 8, out var second));

        await table.Save(roomId, first);
        await table.Save(roomId, second);
        var loaded = await table.Load(roomId);

        Assert.NotNull(loaded);
        Assert.Equal('O', loaded.Squares[8]);
        Assert.Equal(Seat.A, loaded.Next);

        await table.Remove(roomId);
    }
}
