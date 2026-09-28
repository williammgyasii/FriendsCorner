using FriendsCorner.Storage;

namespace FriendsCorner.Tests;

public class ChessTableTests
{
    [Fact]
    public async Task A_played_position_and_who_is_white_come_back_from_Neon()
    {
        var table = new ChessTable(TestDatabase.ConnectionString());
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
        var table = new ChessTable(TestDatabase.ConnectionString());

        Assert.Null(await table.Load(Guid.NewGuid().ToString("N")));
    }
}
