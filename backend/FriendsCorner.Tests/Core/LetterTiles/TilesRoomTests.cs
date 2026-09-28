namespace FriendsCorner.Tests;

public class TilesRoomTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void The_lobby_offers_letter_tiles_for_up_to_four()
    {
        Assert.Contains(new Game(LetterTilesGame.GameId, LobbyEngine.MaxCapacity), LobbyEngine.Games);
    }

    [Fact]
    public void A_four_seat_room_that_starts_tiles_puts_everyone_in_the_game()
    {
        var room = new RoomEngine(new GameCatalog(new WordsAndScoreTests.FakeWords("CAT"), () => new Random(7)));
        Assert.True(room.TryAddSeat(out _));
        room.Apply(Seat.A, new SetCapacity(4), Now);
        for (var i = 0; i < 3; i++)
        {
            Assert.True(room.TryAddSeat(out _));
        }

        room.Apply(Seat.A, new PickGame(LetterTilesGame.GameId), Now);
        foreach (var seat in new[] { Seat.B, Seat.C, Seat.D })
        {
            room.Apply(seat, new SetReady(true), Now);
        }

        room.Apply(Seat.A, new StartGame(), Now);

        Assert.Equal(RoomChange.Lobby | RoomChange.Game, room.Advance(0.05, Now.AddSeconds(3)));
        Assert.Equal(LetterTilesGame.GameId, room.World);
        var game = Assert.IsType<LetterTilesGame>(room.Game);
        Assert.Equal([Seat.A, Seat.B, Seat.C, Seat.D], game.State.Playing);
        Assert.Equal(Seat.A, game.State.ToMove);
    }
}
