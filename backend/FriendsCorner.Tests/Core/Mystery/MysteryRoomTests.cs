namespace FriendsCorner.Tests;

public class MysteryRoomTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void The_lobby_offers_a_mystery_for_two()
    {
        Assert.Contains(new Game(MysteryGame.GameId, 2), LobbyEngine.Games);
    }

    [Fact]
    public void A_finished_countdown_opens_a_mystery_that_is_writing()
    {
        var room = new RoomEngine();
        room.TryAddSeat(out _);
        room.TryAddSeat(out _);
        room.Apply(Seat.A, new PickGame(MysteryGame.GameId), Now);
        room.Apply(Seat.B, new SetReady(true), Now);
        room.Apply(Seat.A, new StartGame(), Now);

        Assert.Equal(RoomChange.Lobby | RoomChange.Game, room.Advance(0.05, Now.AddSeconds(3)));

        Assert.Equal(MysteryGame.GameId, room.World);
        var game = Assert.IsType<MysteryGame>(room.Game);
        Assert.Equal(MysteryPhase.Writing, game.State.Phase);
        Assert.Equal([Seat.A, Seat.B], game.State.Playing);
    }
}
