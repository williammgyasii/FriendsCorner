using FriendsCorner.Core.Accessors;
using FriendsCorner.Infrastructure.Accessors;

namespace FriendsCorner.Tests;

public class ChessTableAccessorTests
{
    [Fact]
    public async Task A_played_position_and_who_is_white_come_back_from_Neon()
    {
        var table = new ChessTableAccessor(await TestDatabase.Contexts());
        var roomId = Guid.NewGuid().ToString("N");
        Assert.True(ChessBoard.Start(Seat.B).TryMove(Seat.B, new ChessMove("e2", "e4"), out var played));

        await table.Save(roomId, played);
        var loaded = await table.Load(roomId);

        Assert.NotNull(loaded);
        Assert.Equal(played.Fen, loaded.Fen);
        Assert.Equal(Seat.B, loaded.White);
        Assert.Equal(Seat.A, loaded.ToMove);

        await table.Remove(roomId);
    }

    [Fact]
    public async Task A_room_with_no_chess_game_loads_nothing()
    {
        var table = new ChessTableAccessor(await TestDatabase.Contexts());

        Assert.Null(await table.Load(Guid.NewGuid().ToString("N")));
    }

    [Fact]
    public async Task Saving_the_same_room_twice_keeps_only_the_latest_position()
    {
        var table = new ChessTableAccessor(await TestDatabase.Contexts());
        var roomId = Guid.NewGuid().ToString("N");
        Assert.True(ChessBoard.Start(Seat.A).TryMove(Seat.A, new ChessMove("e2", "e4"), out var first));
        Assert.True(first.TryMove(Seat.B, new ChessMove("e7", "e5"), out var second));

        await table.Save(roomId, first);
        await table.Save(roomId, second);
        var loaded = await table.Load(roomId);

        Assert.NotNull(loaded);
        Assert.Equal(second.Fen, loaded.Fen);

        await table.Remove(roomId);
    }
}
