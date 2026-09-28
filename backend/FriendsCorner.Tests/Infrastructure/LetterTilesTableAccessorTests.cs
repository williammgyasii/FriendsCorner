using FriendsCorner.Infrastructure.Accessors;

namespace FriendsCorner.Tests;

public class LetterTilesTableAccessorTests
{
    private static readonly WordsAndScoreTests.FakeWords Words = new("AT", "CAT");

    // Two turns in: A played A + blank T for 2, B passed, so it is A's turn again.
    private static LetterTilesState MidGame()
    {
        var start = LetterTilesState.Start([Seat.A, Seat.B], Seat.A, new Random(3));
        var game = new LetterTilesGame(
            start with { Racks = new Dictionary<Seat, IReadOnlyList<Tile>>(start.Racks) { [Seat.A] = [new Tile('A'), Tile.Blank, new Tile('Z')] } },
            Words,
            new Random(3));
        Assert.True(game.TryPlay(Seat.A, new PlayTiles([new PlacedTile(112, new Tile('A')), new PlacedTile(113, Tile.Blank.As('T'))])));
        Assert.True(game.TryPlay(Seat.B, new PassTurn()));
        return game.State;
    }

    private static void AssertSameGame(LetterTilesState expected, LetterTilesState actual)
    {
        Assert.Equal(expected.Board, actual.Board);
        Assert.Equal(expected.Bag, actual.Bag);
        Assert.Equal(expected.Playing, actual.Playing);
        Assert.All(expected.Playing, seat =>
        {
            Assert.Equal(expected.Racks[seat], actual.Racks[seat]);
            Assert.Equal(expected.Scores[seat], actual.Scores[seat]);
        });
        Assert.Equal(
            (expected.ToMove, expected.Starter, expected.ScorelessTurns),
            (actual.ToMove, actual.Starter, actual.ScorelessTurns));
        Assert.Equal(expected.LastPlay?.Seat, actual.LastPlay?.Seat);
        Assert.Equal(expected.LastPlay?.Words, actual.LastPlay?.Words);
        Assert.Equal(expected.LastPlay?.Score, actual.LastPlay?.Score);
        Assert.Equal(expected.Outcome?.Winners, actual.Outcome?.Winners);
    }

    [Fact]
    public async Task A_game_in_progress_comes_back_from_Neon_exactly()
    {
        var table = new LetterTilesTableAccessor(await TestDatabase.Contexts());
        var roomId = Guid.NewGuid().ToString("N");
        var saved = MidGame();

        await table.Save(roomId, saved);
        var loaded = await table.Load(roomId);

        Assert.NotNull(loaded);
        AssertSameGame(saved, loaded);
        Assert.Equal(Tile.Blank.As('T'), loaded.Board[113]);
        Assert.Equal(1, loaded.ScorelessTurns);

        await table.Remove(roomId);
    }

    [Fact]
    public async Task A_finished_game_keeps_its_winners()
    {
        var table = new LetterTilesTableAccessor(await TestDatabase.Contexts());
        var roomId = Guid.NewGuid().ToString("N");
        var game = new LetterTilesGame(MidGame(), Words, new Random(3));
        while (game.State.Outcome is null)
        {
            Assert.True(game.TryPlay(game.State.ToMove, new PassTurn()));
        }

        await table.Save(roomId, game.State);
        await table.Save(roomId, game.State);
        var loaded = await table.Load(roomId);

        Assert.NotNull(loaded);
        AssertSameGame(game.State, loaded);
        Assert.NotNull(loaded.Outcome);

        await table.Remove(roomId);
    }

    [Fact]
    public async Task A_room_with_no_tiles_game_loads_nothing()
    {
        var table = new LetterTilesTableAccessor(await TestDatabase.Contexts());

        Assert.Null(await table.Load(Guid.NewGuid().ToString("N")));
    }

    [Fact]
    public async Task The_game_table_door_brings_back_a_saved_tiles_game()
    {
        var contexts = await TestDatabase.Contexts();
        var tiles = new LetterTilesTableAccessor(contexts);
        var games = new GameTableAccessor(new BoardTableAccessor(contexts), new ChessTableAccessor(contexts), tiles, Words);
        var roomId = Guid.NewGuid().ToString("N");
        var saved = MidGame();
        await tiles.Save(roomId, saved);

        var loaded = Assert.IsType<LetterTilesGame>(await games.Load(roomId));

        AssertSameGame(saved, loaded.State);
        Assert.True(loaded.TryPlay(Seat.A, new PassTurn()));

        await tiles.Remove(roomId);
    }

    [Fact]
    public void A_noted_tiles_game_goes_to_the_tiles_recorder()
    {
        var tiles = new LetterTilesRecorderAccessor();
        var games = new GameRecorderAccessor(new BoardRecorderAccessor(), new ChessRecorderAccessor(), tiles);
        var game = LetterTilesGame.Start([Seat.A, Seat.B], Words, new Random(3));

        games.Note("room-1", game);

        var waiting = Assert.Single(tiles.TakeWaiting());
        Assert.Equal(("room-1", game.State), waiting);
    }
}
