
namespace FriendsCorner.Tests;

public class LobbyEngineTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 20, 0, 0, TimeSpan.Zero);

    private static LobbyEngine ReadyToStart()
    {
        var lobby = new LobbyEngine();
        lobby.Join(Seat.A);
        lobby.Join(Seat.B);
        Assert.True(lobby.TryPick(Seat.A, "chess"));
        Assert.True(lobby.TrySetReady(Seat.B, true));
        return lobby;
    }

    [Fact]
    public void The_first_player_in_is_the_game_master()
    {
        var lobby = new LobbyEngine();

        lobby.Join(Seat.A);
        lobby.Join(Seat.B);

        Assert.Equal(Seat.A, lobby.Host);
        Assert.Equal([Seat.A, Seat.B], lobby.Players);
    }

    [Fact]
    public void When_the_game_master_leaves_the_next_player_in_takes_over()
    {
        var lobby = new LobbyEngine();
        lobby.Join(Seat.A);
        Assert.True(lobby.TrySetCapacity(Seat.A, 3));
        lobby.Join(Seat.B);
        lobby.Join(Seat.C);

        lobby.Leave(Seat.A);

        Assert.Equal(Seat.B, lobby.Host);
        Assert.Equal([Seat.B, Seat.C], lobby.Players);
    }

    [Fact]
    public void An_empty_lobby_has_no_game_master()
    {
        var lobby = new LobbyEngine();
        lobby.Join(Seat.A);

        lobby.Leave(Seat.A);

        Assert.Null(lobby.Host);
    }

    [Fact]
    public void Only_the_game_master_picks_the_game()
    {
        var lobby = new LobbyEngine();
        lobby.Join(Seat.A);
        lobby.Join(Seat.B);

        Assert.False(lobby.TryPick(Seat.B, "chess"));
        Assert.True(lobby.TryPick(Seat.A, "tictactoe"));

        Assert.Equal("tictactoe", lobby.Pick);
    }

    [Fact]
    public void A_game_that_does_not_exist_cannot_be_picked()
    {
        var lobby = new LobbyEngine();
        lobby.Join(Seat.A);

        Assert.False(lobby.TryPick(Seat.A, "poker"));
        Assert.Null(lobby.Pick);
    }

    [Fact]
    public void The_game_master_sets_the_lobby_size_between_two_and_four()
    {
        var lobby = new LobbyEngine();
        lobby.Join(Seat.A);
        lobby.Join(Seat.B);

        Assert.False(lobby.TrySetCapacity(Seat.B, 3));
        Assert.False(lobby.TrySetCapacity(Seat.A, 1));
        Assert.False(lobby.TrySetCapacity(Seat.A, 5));
        Assert.True(lobby.TrySetCapacity(Seat.A, 4));

        Assert.Equal(4, lobby.Capacity);
    }

    [Fact]
    public void The_lobby_cannot_shrink_below_the_players_already_in_it()
    {
        var lobby = new LobbyEngine();
        lobby.Join(Seat.A);
        Assert.True(lobby.TrySetCapacity(Seat.A, 3));
        lobby.Join(Seat.B);
        lobby.Join(Seat.C);

        Assert.False(lobby.TrySetCapacity(Seat.A, 2));
        Assert.Equal(3, lobby.Capacity);
    }

    [Fact]
    public void A_full_lobby_turns_away_the_next_player()
    {
        var lobby = new LobbyEngine();

        Assert.True(lobby.Join(Seat.A));
        Assert.True(lobby.Join(Seat.B));
        Assert.False(lobby.Join(Seat.C));

        Assert.Equal([Seat.A, Seat.B], lobby.Players);
    }

    [Fact]
    public void Start_waits_until_everyone_invited_has_joined()
    {
        var lobby = new LobbyEngine();
        lobby.Join(Seat.A);
        Assert.True(lobby.TryPick(Seat.A, "chess"));

        Assert.False(lobby.CanStart);
        Assert.False(lobby.TryStart(Seat.A, Now));
    }

    [Fact]
    public void Start_waits_until_every_other_player_is_ready()
    {
        var lobby = new LobbyEngine();
        lobby.Join(Seat.A);
        lobby.Join(Seat.B);
        Assert.True(lobby.TryPick(Seat.A, "chess"));

        Assert.False(lobby.CanStart);
        Assert.True(lobby.TrySetReady(Seat.B, true));
        Assert.True(lobby.CanStart);
    }

    [Fact]
    public void Start_waits_for_a_game_to_be_picked()
    {
        var lobby = new LobbyEngine();
        lobby.Join(Seat.A);
        lobby.Join(Seat.B);
        Assert.True(lobby.TrySetReady(Seat.B, true));

        Assert.False(lobby.CanStart);
    }

    [Fact]
    public void Picking_a_different_game_asks_everyone_to_ready_up_again()
    {
        var lobby = ReadyToStart();

        Assert.True(lobby.TryPick(Seat.A, "tictactoe"));

        Assert.False(lobby.IsReady(Seat.B));
        Assert.False(lobby.CanStart);
    }

    [Fact]
    public void Only_the_game_master_starts_the_countdown()
    {
        var lobby = ReadyToStart();

        Assert.False(lobby.TryStart(Seat.B, Now));
        Assert.True(lobby.TryStart(Seat.A, Now));

        Assert.Equal(Now.AddSeconds(3), lobby.CountdownEndsAt);
    }

    [Fact]
    public void The_game_opens_when_the_countdown_runs_out()
    {
        var lobby = ReadyToStart();
        Assert.True(lobby.TryStart(Seat.A, Now));

        Assert.False(lobby.TryFinishCountdown(Now.AddSeconds(2), out _));
        Assert.True(lobby.TryFinishCountdown(Now.AddSeconds(3), out var world));

        Assert.Equal("chess", world);
        Assert.Null(lobby.CountdownEndsAt);
    }

    [Fact]
    public void Someone_leaving_during_the_countdown_calls_it_off()
    {
        var lobby = ReadyToStart();
        Assert.True(lobby.TryStart(Seat.A, Now));

        lobby.Leave(Seat.B);

        Assert.Null(lobby.CountdownEndsAt);
        Assert.False(lobby.TryFinishCountdown(Now.AddSeconds(5), out _));
    }

    [Fact]
    public void Extra_players_beyond_a_two_player_game_watch()
    {
        var lobby = new LobbyEngine();
        lobby.Join(Seat.A);
        Assert.True(lobby.TrySetCapacity(Seat.A, 3));
        lobby.Join(Seat.B);
        lobby.Join(Seat.C);
        Assert.True(lobby.TryPick(Seat.A, "chess"));

        Assert.Equal([Seat.A, Seat.B], lobby.Playing);
        Assert.Equal([Seat.C], lobby.Watching);
    }

    [Fact]
    public void Each_player_shares_whether_their_camera_and_mic_are_on()
    {
        var lobby = new LobbyEngine();
        lobby.Join(Seat.A);

        lobby.SetMedia(Seat.A, camera: false, mic: true);

        Assert.Equal(new Media(Camera: false, Mic: true), lobby.MediaOf(Seat.A));
    }
}
