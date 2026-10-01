using FriendsCorner.Infrastructure.Accessors;

namespace FriendsCorner.Tests;

public class MysteryTableAccessorTests
{
    private static readonly WordsAndScoreTests.FakeWords Words = new("AT");

    // Two leads open and A has picked s2; B has not picked.
    private static MysteryState MidInvestigation()
    {
        var game = MysteryGame.Start([Seat.A, Seat.B]);
        Assert.True(game.TryReceive(1, CaseFixture.Fair()));
        Assert.True(game.TryPlay(Seat.A, new OpenLead("c1")));
        Assert.True(game.TryPlay(Seat.B, new OpenLead("t5")));
        Assert.True(game.TryPlay(Seat.A, new Accuse("s2")));
        return game.State;
    }

    private static void AssertSameGame(MysteryState expected, MysteryState actual)
    {
        Assert.Equal((expected.Phase, expected.Request, expected.LeadsLeftFor(Seat.A)), (actual.Phase, actual.Request, actual.LeadsLeftFor(Seat.A)));
        Assert.Equal(expected.Playing, actual.Playing);
        Assert.Equal(expected.Opened, actual.Opened);
        Assert.Equal(expected.Picks.OrderBy(pick => pick.Key), actual.Picks.OrderBy(pick => pick.Key));
        Assert.Equivalent(expected.Case, actual.Case, strict: true);
    }

    [Fact]
    public async Task A_game_mid_investigation_comes_back_from_Neon_exactly()
    {
        var table = new MysteryTableAccessor(await TestDatabase.Contexts());
        var roomId = Guid.NewGuid().ToString("N");
        var saved = MidInvestigation();

        await table.Save(roomId, saved);
        await table.Save(roomId, saved);
        var loaded = await table.Load(roomId);

        Assert.NotNull(loaded);
        AssertSameGame(saved, loaded);
        Assert.True(loaded.Case!.Leads.Single(lead => lead.Id == "t5").Lie);

        await table.Remove(roomId);
    }

    [Fact]
    public async Task A_hard_race_with_its_clock_comes_back_exactly()
    {
        var table = new MysteryTableAccessor(await TestDatabase.Contexts());
        var roomId = Guid.NewGuid().ToString("N");
        var game = MysteryGame.Start([Seat.A, Seat.B], new MysterySettings(MysteryLevel.Hard, MysteryMode.Race));
        Assert.True(game.TryReceive(1, CaseFixture.Fair() with { Minutes = 9, Mood = CaseMood.Garden }));
        Assert.True(game.TryAdvance(MysteryPlay.T0));
        Assert.True(game.TryPlay(Seat.A, new OpenLead("c1")));
        Assert.True(game.TryPlay(Seat.B, new Accuse("s1")));

        await table.Save(roomId, game.State);
        var loaded = await table.Load(roomId);

        Assert.NotNull(loaded);
        AssertSameGame(game.State, loaded);
        Assert.Equal(game.State.Settings, loaded.Settings);
        Assert.Equal(game.State.EndsAt, loaded.EndsAt);
        Assert.Equal((9, CaseMood.Garden), (loaded.Case!.Minutes, loaded.Case.Mood));
        Assert.True(loaded.IsOut(Seat.B));

        await table.Remove(roomId);
    }

    [Fact]
    public async Task A_row_it_cannot_read_loads_nothing()
    {
        var contexts = await TestDatabase.Contexts();
        var table = new MysteryTableAccessor(contexts);
        var roomId = Guid.NewGuid().ToString("N");
        await using (var db = await contexts.CreateDbContextAsync())
        {
            db.Mystery.Add(new FriendsCorner.Infrastructure.Persistence.MysteryRow { RoomId = roomId, State = """{"phase":"investigating","opened":["c1"]}""" });
            await db.SaveChangesAsync();
        }

        Assert.Null(await table.Load(roomId));

        await table.Remove(roomId);
    }

    [Fact]
    public async Task A_room_with_no_mystery_loads_nothing()
    {
        var table = new MysteryTableAccessor(await TestDatabase.Contexts());

        Assert.Null(await table.Load(Guid.NewGuid().ToString("N")));
    }

    [Fact]
    public async Task The_game_table_door_brings_back_a_mystery_that_can_be_played_on()
    {
        var contexts = await TestDatabase.Contexts();
        var mystery = new MysteryTableAccessor(contexts);
        var games = Games(contexts, mystery);
        var roomId = Guid.NewGuid().ToString("N");
        var saved = MidInvestigation();
        await mystery.Save(roomId, saved);

        var loaded = Assert.IsType<MysteryGame>(await games.Load(roomId));

        AssertSameGame(saved, loaded.State);
        Assert.True(loaded.TryPlay(Seat.B, new OpenLead("t3")));

        await mystery.Remove(roomId);
    }

    [Fact]
    public async Task A_game_saved_while_writing_comes_back_failed_so_the_players_can_rematch()
    {
        var contexts = await TestDatabase.Contexts();
        var mystery = new MysteryTableAccessor(contexts);
        var roomId = Guid.NewGuid().ToString("N");
        await mystery.Save(roomId, MysteryGame.Start([Seat.A, Seat.B]).State);

        var loaded = Assert.IsType<MysteryGame>(await Games(contexts, mystery).Load(roomId));

        Assert.Equal(MysteryPhase.Failed, loaded.State.Phase);
        Assert.True(loaded.TryRematch());

        await mystery.Remove(roomId);
    }

    [Fact]
    public void A_noted_mystery_goes_to_the_mystery_recorder()
    {
        var mystery = new MysteryRecorderAccessor();
        var games = new GameRecorderAccessor(new BoardRecorderAccessor(), new ChessRecorderAccessor(), new LetterTilesRecorderAccessor(), mystery);
        var game = MysteryGame.Start([Seat.A, Seat.B]);

        games.Note("room-1", game);

        Assert.Equal(("room-1", game.State), Assert.Single(mystery.TakeWaiting()));
    }

    private static GameTableAccessor Games(Microsoft.EntityFrameworkCore.IDbContextFactory<FriendsCorner.Infrastructure.Persistence.FriendsCornerDb> contexts, MysteryTableAccessor mystery) =>
        new(new BoardTableAccessor(contexts), new ChessTableAccessor(contexts), new LetterTilesTableAccessor(contexts), mystery, Words);
}
