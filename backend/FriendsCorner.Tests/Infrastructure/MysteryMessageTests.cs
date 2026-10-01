using System.Text.Json;
using FriendsCorner.Infrastructure.Accessors;

namespace FriendsCorner.Tests;

// What each seat is told about a murder mystery. The solution, the proofs,
// the lie, and every closed lead stay on the server until the reveal.
public class MysteryMessageTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 20, 0, 0, TimeSpan.Zero);
    private readonly StateMessageAccessor _state = new();

    private static (RoomEngine Room, MysteryGame Game) MysteryRoom()
    {
        var room = new RoomEngine();
        Assert.True(room.TryAddSeat(out _));
        Assert.True(room.TryAddSeat(out _));
        var game = MysteryGame.Start([Seat.A, Seat.B]);
        room.Restore(game);
        return (room, game);
    }

    private static (RoomEngine Room, MysteryGame Game) MidInvestigation()
    {
        var (room, game) = MysteryRoom();
        Assert.True(game.TryReceive(1, CaseFixture.Fair()));
        Assert.True(game.TryPlay(Seat.A, new OpenLead("c1")));
        Assert.True(game.TryPlay(Seat.B, new OpenLead("t4")));
        Assert.True(game.TryPlay(Seat.A, new Accuse("s2")));
        return (room, game);
    }

    private JsonElement Mystery(RoomEngine room, Seat you) =>
        JsonDocument.Parse(_state.Write(room, you, Now)).RootElement.GetProperty("mystery").Clone();

    [Fact]
    public void While_writing_the_section_is_the_phase_and_empty_case()
    {
        var (room, _) = MysteryRoom();

        Assert.Equal(
            """{"phase":"writing","level":"easy","mode":"together","mood":null,"title":null,"setting":null,"victim":null,"suspects":[],"places":[],"leads":[],"leadsLeft":8,"picks":{"A":null,"B":null},"out":[],"endsInMs":null,"locksInMs":null,"outcome":null}""",
            Mystery(room, Seat.A).GetRawText());
    }

    [Fact]
    public void A_failed_write_says_failed()
    {
        var (room, game) = MysteryRoom();
        Assert.True(game.TryFail(1));

        Assert.Equal("failed", Mystery(room, Seat.B).GetProperty("phase").GetString());
    }

    [Fact]
    public void Mid_investigation_the_case_open_leads_and_picks_match_the_wire_format()
    {
        var (room, _) = MidInvestigation();

        var mystery = Mystery(room, Seat.B);

        Assert.Equal("investigating", mystery.GetProperty("phase").GetString());
        Assert.Equal("Death at the Lantern Inn", mystery.GetProperty("title").GetString());
        Assert.Equal("""{"name":"Edmund Hale","found":"Found at the foot of the cellar stairs at midnight."}""", mystery.GetProperty("victim").GetRawText());
        Assert.Equal(
            """{"id":"s1","name":"Ada Finch","role":"Cook","motive":"Edmund owed her wages.","alibi":"In the kitchen all evening."}""",
            mystery.GetProperty("suspects")[0].GetRawText());
        Assert.Equal("""[{"id":"p1","name":"Kitchen"},{"id":"p2","name":"Cellar"},{"id":"p3","name":"Stables"}]""", mystery.GetProperty("places").GetRawText());
        Assert.Equal("smoke", mystery.GetProperty("mood").GetString());
        Assert.Equal("""{"id":"c1","kind":"clue","about":"p1","text":"The bread oven was lit at eleven and never left alone.","by":"A"}""", mystery.GetProperty("leads")[0].GetRawText());
        Assert.Equal("""{"id":"t4","kind":"statement","about":"s2","text":"I heard footsteps on the cellar stairs.","by":"B"}""", mystery.GetProperty("leads")[9].GetRawText());
        Assert.Equal("""{"id":"c2","kind":"clue","about":"p1","text":null,"by":null}""", mystery.GetProperty("leads")[1].GetRawText());
        Assert.Equal(12, mystery.GetProperty("leads").EnumerateArray().Count(lead => lead.GetProperty("text").ValueKind == JsonValueKind.Null));
        Assert.Equal(6, mystery.GetProperty("leadsLeft").GetInt32());
        Assert.Equal("""{"A":"s2","B":null}""", mystery.GetProperty("picks").GetRawText());
        Assert.Equal(JsonValueKind.Null, mystery.GetProperty("outcome").ValueKind);
    }

    [Fact]
    public void Before_the_reveal_no_message_carries_a_secret()
    {
        var (room, _) = MidInvestigation();
        var fair = CaseFixture.Fair();
        var closed = fair.Leads.Where(lead => lead.Id is not ("c1" or "t4")).Select(lead => lead.Text);

        foreach (var seat in new[] { Seat.A, Seat.B })
        {
            var json = _state.Write(room, seat, Now);

            Assert.DoesNotContain(fair.Solution.Story, json);
            Assert.DoesNotContain(fair.Solution.How, json);
            Assert.DoesNotContain(fair.Solution.Why, json);
            Assert.DoesNotContain("killer", json);
            Assert.DoesNotContain("proof", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("lie", json);
            Assert.All(closed, text => Assert.DoesNotContain(text, json));
        }
    }

    [Fact]
    public void After_the_reveal_the_outcome_is_shared()
    {
        var (room, game) = MidInvestigation();
        game.Agree("s3");
        var solution = CaseFixture.Fair().Solution;

        var mystery = Mystery(room, Seat.A);

        Assert.Equal("revealed", mystery.GetProperty("phase").GetString());
        Assert.Equal(
            JsonSerializer.Serialize(new
            {
                killer = "s3",
                how = solution.How,
                why = solution.Why,
                story = solution.Story,
                accused = "s3",
                right = true,
                score = 160,
                winner = (string?)null,
                timedOut = false,
            }),
            mystery.GetProperty("outcome").GetRawText());
    }

    [Fact]
    public void A_hard_case_sends_the_time_left_and_its_settings()
    {
        var room = TwoSeats();
        var game = MysteryGame.Start([Seat.A, Seat.B], new MysterySettings(MysteryLevel.Hard, MysteryMode.Together));
        room.Restore(game);
        game.TryReceive(1, CaseFixture.Fair() with { Minutes = 12 });
        game.TryAdvance(Now.AddSeconds(-30));

        var mystery = Mystery(room, Seat.A);

        Assert.Equal("hard", mystery.GetProperty("level").GetString());
        Assert.Equal("together", mystery.GetProperty("mode").GetString());
        Assert.Equal(690_000, mystery.GetProperty("endsInMs").GetInt32());
        Assert.Equal(6, mystery.GetProperty("leadsLeft").GetInt32());
    }

    [Fact]
    public void The_final_answer_sends_the_time_to_the_lock()
    {
        var (room, game) = MysteryRoom();
        game.TryReceive(1, CaseFixture.Fair());
        game.TryPlay(Seat.A, new Accuse("s3"));
        game.TryPlay(Seat.B, new Accuse("s3"));
        game.TryAdvance(Now.AddSeconds(-2));

        var mystery = Mystery(room, Seat.B);

        Assert.Equal("accusing", mystery.GetProperty("phase").GetString());
        Assert.Equal(3000, mystery.GetProperty("locksInMs").GetInt32());
        Assert.Equal(JsonValueKind.Null, mystery.GetProperty("endsInMs").ValueKind);
    }

    [Fact]
    public void A_race_tells_each_seat_only_its_own_leads_and_who_is_out()
    {
        var room = TwoSeats();
        var game = MysteryGame.Start([Seat.A, Seat.B], new MysterySettings(MysteryLevel.Easy, MysteryMode.Race));
        room.Restore(game);
        game.TryReceive(1, CaseFixture.Fair());
        game.TryPlay(Seat.A, new OpenLead("c1"));
        game.TryPlay(Seat.A, new Accuse("s1"));

        var toB = Mystery(room, Seat.B);
        var toA = Mystery(room, Seat.A);

        Assert.Equal("race", toB.GetProperty("mode").GetString());
        Assert.Equal("""{"id":"c1","kind":"clue","about":"p1","text":null,"by":null}""", toB.GetProperty("leads")[0].GetRawText());
        Assert.Equal("""{"A":null,"B":null}""", toB.GetProperty("picks").GetRawText());
        Assert.Equal("""["A"]""", toB.GetProperty("out").GetRawText());
        Assert.Equal(8, toB.GetProperty("leadsLeft").GetInt32());
        Assert.DoesNotContain(CaseFixture.Fair().Leads[0].Text, _state.Write(room, Seat.B, Now));
        Assert.Equal("""{"A":"s1","B":null}""", toA.GetProperty("picks").GetRawText());
        Assert.Equal(7, toA.GetProperty("leadsLeft").GetInt32());
    }

    [Fact]
    public void A_race_outcome_names_the_winner()
    {
        var room = TwoSeats();
        var game = MysteryGame.Start([Seat.A, Seat.B], new MysterySettings(MysteryLevel.Easy, MysteryMode.Race));
        room.Restore(game);
        game.TryReceive(1, CaseFixture.Fair());
        game.TryPlay(Seat.B, new Accuse("s3"));

        var outcome = Mystery(room, Seat.A).GetProperty("outcome");

        Assert.Equal("B", outcome.GetProperty("winner").GetString());
        Assert.Equal(180, outcome.GetProperty("score").GetInt32());
    }

    [Fact]
    public void The_lobby_carries_the_mystery_settings()
    {
        var room = TwoSeats();
        room.Apply(Seat.A, new SetMysterySettings(new MysterySettings(MysteryLevel.Hard, MysteryMode.Race)), Now);

        using var state = JsonDocument.Parse(_state.Write(room, Seat.B, Now));

        Assert.Equal("""{"level":"hard","mode":"race"}""", state.RootElement.GetProperty("lobby").GetProperty("mystery").GetRawText());
    }

    private static RoomEngine TwoSeats()
    {
        var room = new RoomEngine();
        Assert.True(room.TryAddSeat(out _));
        Assert.True(room.TryAddSeat(out _));
        return room;
    }

    [Fact]
    public void A_letter_tiles_room_gains_only_a_null_mystery()
    {
        var room = new RoomEngine();
        Assert.True(room.TryAddSeat(out _));
        Assert.True(room.TryAddSeat(out _));
        room.Restore(new LetterTilesGame(
            LetterTilesState.Start([Seat.A, Seat.B], Seat.A, new Random(7)), new WordsAndScoreTests.FakeWords("AT"), new Random(7)));

        using var state = JsonDocument.Parse(_state.Write(room, Seat.A, Now));

        Assert.Equal(JsonValueKind.Null, state.RootElement.GetProperty("mystery").ValueKind);
        Assert.Equal(JsonValueKind.Object, state.RootElement.GetProperty("tiles").ValueKind);
    }
}
