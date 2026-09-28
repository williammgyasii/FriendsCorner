using FriendsCorner.Storage;

namespace FriendsCorner.Tests;

public class BoardTableTests
{
    [Fact]
    public async Task An_empty_board_is_nine_empty_squares_when_read_back()
    {
        var table = new BoardTable(ConnectionString());
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
        var table = new BoardTable(ConnectionString());
        var roomId = Guid.NewGuid().ToString("N");
        Assert.True(Board.Empty().TryPlace(Seat.A, 4, out var played));

        await table.Save(roomId, played);
        var loaded = await table.Load(roomId);

        Assert.NotNull(loaded);
        Assert.Equal('X', loaded.Squares[4]);
        Assert.Equal(Seat.B, loaded.Next);

        await table.Remove(roomId);
    }

    private static string ConnectionString() => TestDatabase.ConnectionString();
}
