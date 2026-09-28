namespace FriendsCorner.Tests;

public class RoomCommandTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 20, 0, 0, TimeSpan.Zero);

    private static RoomEngine TwoInTheLobby()
    {
        var room = new RoomEngine();
        Assert.True(room.TryAddSeat(out _));
        Assert.True(room.TryAddSeat(out _));
        return room;
    }

    [Fact]
    public void Steering_moves_the_player_on_the_next_tick_without_a_broadcast()
    {
        var room = new RoomEngine();
        Assert.True(room.TryAddSeat(out Seat seat));

        var change = room.Apply(seat, new Steer(1, 0), Now);

        Assert.Equal(RoomChange.None, change);
        room.Tick(1);
        Assert.Equal(400, room.Positions[seat].X, precision: 5);
    }

    [Fact]
    public void The_game_master_picks_the_game()
    {
        var room = TwoInTheLobby();

        var change = room.Apply(Seat.A, new PickGame("chess"), Now);

        Assert.Equal(RoomChange.Lobby, change);
        Assert.Equal("chess", room.Lobby.Pick);
    }

    [Fact]
    public void A_pick_from_someone_else_changes_nothing()
    {
        var room = TwoInTheLobby();

        var change = room.Apply(Seat.B, new PickGame("chess"), Now);

        Assert.Equal(RoomChange.None, change);
        Assert.Null(room.Lobby.Pick);
    }

    [Fact]
    public void Ready_readies_the_player()
    {
        var room = TwoInTheLobby();

        var change = room.Apply(Seat.B, new SetReady(true), Now);

        Assert.Equal(RoomChange.Lobby, change);
        Assert.True(room.Lobby.IsReady(Seat.B));
    }

    [Fact]
    public void A_bigger_lobby_lets_a_third_player_take_seat_C()
    {
        var room = TwoInTheLobby();

        var change = room.Apply(Seat.A, new SetCapacity(3), Now);

        Assert.Equal(RoomChange.Lobby, change);
        Assert.True(room.TryAddSeat(out Seat third));
        Assert.Equal(Seat.C, third);
    }

    [Fact]
    public void Sharing_media_sets_camera_and_mic()
    {
        var room = TwoInTheLobby();

        var change = room.Apply(Seat.B, new ShareMedia(Camera: false, Mic: true), Now);

        Assert.Equal(RoomChange.Lobby, change);
        Assert.Equal(new Media(Camera: false, Mic: true), room.Lobby.MediaOf(Seat.B));
    }

    [Fact]
    public void Start_begins_the_countdown_and_the_clock_opens_the_game()
    {
        var room = TwoInTheLobby();
        room.Apply(Seat.A, new PickGame("chess"), Now);
        room.Apply(Seat.B, new SetReady(true), Now);

        var change = room.Apply(Seat.A, new StartGame(), Now);

        Assert.Equal(RoomChange.Lobby, change);
        Assert.Null(room.World);
        Assert.False(room.TryFinishCountdown(Now.AddSeconds(1)));
        Assert.True(room.TryFinishCountdown(Now.AddSeconds(3)));
        Assert.Equal("chess", room.World);
    }

    [Fact]
    public void Placing_a_mark_changes_the_tic_tac_toe_board()
    {
        var room = new RoomEngine();
        Assert.True(room.TryAddSeat(out Seat seat));
        Assert.True(room.TryLaunch("tictactoe"));

        var change = room.Apply(seat, new Place(0), Now);

        Assert.Equal(RoomChange.Game, change);
        Assert.Equal('X', room.RunningTicTacToe().Squares[0]);
    }

    [Fact]
    public void A_legal_chess_move_changes_the_chess_board()
    {
        var room = TwoInTheLobby();
        Assert.True(room.TryLaunch("chess"));

        var change = room.Apply(Seat.A, new MoveChess(new ChessMove("e2", "e4")), Now);

        Assert.Equal(RoomChange.Game, change);
    }

    [Fact]
    public void An_illegal_chess_move_changes_nothing()
    {
        var room = TwoInTheLobby();
        Assert.True(room.TryLaunch("chess"));

        var change = room.Apply(Seat.A, new MoveChess(new ChessMove("e2", "e5")), Now);

        Assert.Equal(RoomChange.None, change);
    }

    [Fact]
    public void A_rematch_before_the_game_ends_changes_nothing()
    {
        var room = TwoInTheLobby();
        Assert.True(room.TryLaunch("tictactoe"));

        Assert.Equal(RoomChange.None, room.Apply(Seat.A, new Rematch(), Now));
    }

    [Fact]
    public void A_clock_step_moves_players_and_changes_nothing_to_save()
    {
        var room = new RoomEngine();
        Assert.True(room.TryAddSeat(out Seat seat));
        room.Apply(seat, new Steer(1, 0), Now);

        var change = room.Advance(0.5, Now);

        Assert.Equal(RoomChange.None, change);
        Assert.Equal(320, room.Positions[seat].X, precision: 5);
    }

    [Theory]
    [InlineData("chess", RoomChange.Lobby | RoomChange.Game)]
    [InlineData("tictactoe", RoomChange.Lobby | RoomChange.Game)]
    [InlineData("room", RoomChange.Lobby)]
    public void The_clock_step_that_ends_the_countdown_opens_the_picked_game(string game, RoomChange expected)
    {
        var room = TwoInTheLobby();
        room.Apply(Seat.A, new PickGame(game), Now);
        room.Apply(Seat.B, new SetReady(true), Now);
        room.Apply(Seat.A, new StartGame(), Now);

        Assert.Equal(RoomChange.None, room.Advance(0.05, Now.AddSeconds(1)));
        Assert.Equal(expected, room.Advance(0.05, Now.AddSeconds(3)));
        Assert.Equal(game, room.World);
    }

    [Theory]
    [InlineData(Seat.A, Seat.B)]
    [InlineData(Seat.B, Seat.A)]
    public void The_face_call_pairs_A_with_B(Seat from, Seat to)
    {
        Assert.Equal(to, RoomEngine.CallPartner(from));
    }

    [Fact]
    public void Watchers_have_no_call_partner()
    {
        Assert.Null(RoomEngine.CallPartner(Seat.C));
    }
}
