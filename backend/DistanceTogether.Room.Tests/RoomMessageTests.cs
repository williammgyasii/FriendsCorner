using DistanceTogether.Server;

namespace DistanceTogether.Tests;

public class RoomMessageTests
{
    [Fact]
    public void A_signal_is_forwarded_and_does_not_change_direction()
    {
        var room = new Room();
        Assert.True(room.TryAddSeat(out Seat seat));
        Assert.True(room.TrySetDirection(seat, 1, 0));

        var applied = RoomMessage.Apply(room, seat, """{"type":"signal","payload":{"kind":"offer"}}""");

        Assert.NotNull(applied.Forward);
        Assert.Contains("signal", applied.Forward);
        Assert.False(applied.OpenedWorld);
        room.Tick(1);
        Assert.Equal(400, room.Positions[seat].X, precision: 5);
    }

    [Fact]
    public void A_direction_message_sets_the_direction()
    {
        var room = new Room();
        Assert.True(room.TryAddSeat(out Seat seat));

        var applied = RoomMessage.Apply(room, seat, """{"type":"direction","x":1,"y":0}""");

        Assert.Null(applied.Forward);
        Assert.False(applied.OpenedWorld);
        room.Tick(1);
        Assert.Equal(400, room.Positions[seat].X, precision: 5);
    }

    [Fact]
    public void A_launch_message_opens_the_world_and_is_not_forwarded()
    {
        var room = new Room();
        Assert.True(room.TryAddSeat(out Seat seat));

        var applied = RoomMessage.Apply(room, seat, """{"type":"launch","world":"room"}""");

        Assert.Null(applied.Forward);
        Assert.True(applied.OpenedWorld);
        Assert.Equal("room", room.World);
    }

    [Fact]
    public void A_place_message_marks_the_board_and_is_not_forwarded()
    {
        var room = new Room();
        Assert.True(room.TryAddSeat(out Seat seat));
        Assert.True(room.TryLaunch("tictactoe"));

        var applied = RoomMessage.Apply(room, seat, """{"type":"place","square":0}""");

        Assert.Null(applied.Forward);
        Assert.True(applied.ChangedBoard);
        Assert.Equal('X', room.TicTacToe!.Squares[0]);
        Assert.Equal(Seat.B, room.TicTacToe.Next);
    }
}
