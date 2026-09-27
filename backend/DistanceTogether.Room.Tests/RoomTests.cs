using DistanceTogether;

namespace DistanceTogether.Tests;

public class RoomTests
{
    [Fact]
    public void Moving_right_for_one_second_moves_seat_A_and_leaves_seat_B()
    {
        var room = new Room();
        Assert.True(room.TryAddSeat(out Seat seatA));
        Assert.True(room.TryAddSeat(out Seat seatB));

        Assert.True(room.TrySetDirection(seatA, 1, 0));
        room.Tick(1);

        Assert.Equal(400, room.Positions[seatA].X, precision: 5);
        Assert.Equal(160, room.Positions[seatA].Y, precision: 5);
        Assert.Equal(240, room.Positions[seatB].X, precision: 5);
        Assert.Equal(160, room.Positions[seatB].Y, precision: 5);
    }

    [Fact]
    public void A_diagonal_direction_moves_the_same_distance_as_a_straight_one()
    {
        var room = new Room();
        Assert.True(room.TryAddSeat(out Seat seatA));
        Assert.True(room.TrySetDirection(seatA, 1, 1));

        room.Tick(1);

        var position = room.Positions[seatA];
        var dx = position.X - 240;
        var dy = position.Y - 160;
        var distance = Math.Sqrt((dx * dx) + (dy * dy));
        Assert.Equal(160, distance, precision: 3);
    }

    [Fact]
    public void The_floor_stops_a_seat_at_the_right_edge()
    {
        var room = new Room();
        Assert.True(room.TryAddSeat(out Seat seatA));
        Assert.True(room.TrySetDirection(seatA, 1, 0));

        room.Tick(2);

        Assert.Equal(480, room.Positions[seatA].X, precision: 5);
        Assert.Equal(160, room.Positions[seatA].Y, precision: 5);
    }

    [Fact]
    public void A_zero_direction_leaves_the_seat_at_the_center()
    {
        var room = new Room();
        Assert.True(room.TryAddSeat(out Seat seatA));
        Assert.True(room.TrySetDirection(seatA, 0, 0));

        room.Tick(1);

        Assert.Equal(240, room.Positions[seatA].X, precision: 5);
        Assert.Equal(160, room.Positions[seatA].Y, precision: 5);
    }

    [Fact]
    public void A_direction_outside_the_allowed_range_is_refused()
    {
        var room = new Room();
        Assert.True(room.TryAddSeat(out Seat seatA));
        Assert.True(room.TrySetDirection(seatA, 1, 0));

        Assert.False(room.TrySetDirection(seatA, 2, 0));
        room.Tick(1);

        Assert.Equal(400, room.Positions[seatA].X, precision: 5);
        Assert.Equal(160, room.Positions[seatA].Y, precision: 5);
    }

    [Fact]
    public void A_freed_seat_is_no_longer_in_the_positions()
    {
        var room = new Room();
        Assert.True(room.TryAddSeat(out Seat seatA));
        Assert.True(room.TryAddSeat(out Seat seatB));
        Assert.True(room.TrySetDirection(seatA, 1, 0));

        room.Free(seatA);
        room.Tick(1);

        Assert.False(room.Positions.ContainsKey(seatA));
        Assert.Equal(240, room.Positions[seatB].X, precision: 5);
        Assert.Equal(160, room.Positions[seatB].Y, precision: 5);
    }

    [Fact]
    public void A_third_seat_is_refused()
    {
        var room = new Room();
        Assert.True(room.TryAddSeat(out Seat seatA));
        Assert.True(room.TryAddSeat(out Seat seatB));

        Assert.False(room.TryAddSeat(out _));
        Assert.Equal(Seat.A, seatA);
        Assert.Equal(Seat.B, seatB);
    }

    [Fact]
    public void Launching_the_room_opens_that_world()
    {
        var room = new Room();

        Assert.Null(room.World);
        Assert.False(room.TryLaunch("cards"));
        Assert.Null(room.World);

        Assert.True(room.TryLaunch("room"));
        Assert.Equal("room", room.World);
        Assert.True(room.TryLaunch("room"));
        Assert.False(room.TryLaunch("cards"));
        Assert.Equal("room", room.World);
    }
}
