namespace FriendsCorner.Tests;

// Levels, the hard clock, and the together final answer. Time only moves
// through TryAdvance, as the room's tick would move it.
public class MysteryClockTests
{
    private static readonly DateTimeOffset T0 = MysteryPlay.T0;
    private static readonly MysterySettings Hard = new(MysteryLevel.Hard, MysteryMode.Together);

    private static MysteryGame Investigating(MysterySettings? settings = null, Case? @case = null)
    {
        var game = MysteryGame.Start([Seat.A, Seat.B], settings);
        Assert.True(game.TryReceive(1, @case ?? CaseFixture.Fair()));
        return game;
    }

    [Fact]
    public void Hard_has_six_leads()
    {
        var game = Investigating(Hard);
        foreach (var lead in new[] { "c1", "c2", "c3", "c4", "c5", "c6" })
        {
            Assert.True(game.TryPlay(Seat.A, new OpenLead(lead)));
        }

        Assert.False(game.TryPlay(Seat.A, new OpenLead("t1")));
        Assert.Equal(0, game.State.LeadsLeftFor(Seat.A));
    }

    [Fact]
    public void Easy_has_no_clock()
    {
        var game = Investigating();

        Assert.False(game.TryAdvance(T0));

        Assert.Null(game.State.EndsAt);
    }

    [Fact]
    public void The_hard_clock_starts_on_the_first_tick_after_the_case()
    {
        var game = Investigating(Hard, CaseFixture.Fair() with { Minutes = 12 });

        Assert.True(game.TryAdvance(T0));
        Assert.False(game.TryAdvance(T0.AddSeconds(1)));

        Assert.Equal(T0.AddMinutes(12), game.State.EndsAt);
    }

    [Fact]
    public void The_clock_does_not_start_while_writing()
    {
        var game = MysteryGame.Start([Seat.A, Seat.B], Hard);

        Assert.False(game.TryAdvance(T0));

        Assert.Null(game.State.EndsAt);
    }

    [Theory]
    [InlineData(60, 20)]
    [InlineData(1, 5)]
    public void The_writers_minutes_are_clamped(int written, int used)
    {
        var game = Investigating(Hard, CaseFixture.Fair() with { Minutes = written });

        game.TryAdvance(T0);

        Assert.Equal(T0.AddMinutes(used), game.State.EndsAt);
    }

    [Fact]
    public void Running_out_of_time_reveals_as_timed_out()
    {
        var game = Investigating(Hard, CaseFixture.Fair() with { Minutes = 10 });
        game.TryAdvance(T0);
        game.TryPlay(Seat.A, new Accuse("s3"));

        Assert.False(game.TryAdvance(T0.AddMinutes(10).AddSeconds(-1)));
        Assert.True(game.TryAdvance(T0.AddMinutes(10)));

        Assert.Equal(MysteryPhase.Revealed, game.State.Phase);
        var outcome = game.View(Seat.A).Outcome!;
        Assert.True(outcome.TimedOut);
        Assert.Null(outcome.Accused);
        Assert.False(outcome.Right);
        Assert.Equal(0, outcome.Score);
    }

    [Fact]
    public void Solving_a_hard_case_adds_the_bonus()
    {
        var game = Investigating(Hard);
        foreach (var lead in new[] { "c1", "t2", "t3", "c5" })
        {
            game.TryPlay(Seat.A, new OpenLead(lead));
        }

        game.Agree("s3");

        Assert.Equal(100 + 20 + 50, game.View(Seat.A).Outcome!.Score);
    }

    [Fact]
    public void Matching_picks_start_the_final_answer()
    {
        var game = Investigating();

        game.TryPlay(Seat.A, new Accuse("s3"));
        game.TryPlay(Seat.B, new Accuse("s3"));

        Assert.Equal(MysteryPhase.Accusing, game.State.Phase);
        Assert.Null(game.State.LocksAt);
        Assert.True(game.TryAdvance(T0));
        Assert.Equal(T0 + MysteryGame.FinalAnswer, game.State.LocksAt);
        Assert.Equal(TimeSpan.FromSeconds(5), MysteryGame.FinalAnswer);
    }

    [Fact]
    public void The_lock_reveals()
    {
        var game = Investigating();
        game.TryPlay(Seat.A, new Accuse("s3"));
        game.TryPlay(Seat.B, new Accuse("s3"));
        game.TryAdvance(T0);

        Assert.False(game.TryAdvance(T0.AddSeconds(4.9)));
        Assert.Equal(MysteryPhase.Accusing, game.State.Phase);
        Assert.True(game.TryAdvance(T0.AddSeconds(5)));

        Assert.Equal(MysteryPhase.Revealed, game.State.Phase);
        Assert.Equal("s3", game.View(Seat.A).Outcome!.Accused);
    }

    [Fact]
    public void Holding_on_cancels_the_final_answer()
    {
        var game = Investigating();
        game.TryPlay(Seat.A, new Accuse("s3"));
        game.TryPlay(Seat.B, new Accuse("s3"));
        game.TryAdvance(T0);

        Assert.True(game.TryPlay(Seat.B, new Withdraw()));

        Assert.Equal(MysteryPhase.Investigating, game.State.Phase);
        Assert.False(game.State.Picks.ContainsKey(Seat.B));
        Assert.Equal("s3", game.State.Picks[Seat.A]);
        Assert.Null(game.State.LocksAt);
        Assert.False(game.TryAdvance(T0.AddSeconds(5)));
    }

    [Fact]
    public void Switching_cancels_the_final_answer()
    {
        var game = Investigating();
        game.TryPlay(Seat.A, new Accuse("s3"));
        game.TryPlay(Seat.B, new Accuse("s3"));
        game.TryAdvance(T0);

        Assert.True(game.TryPlay(Seat.A, new Accuse("s2")));

        Assert.Equal(MysteryPhase.Investigating, game.State.Phase);
        Assert.Null(game.State.LocksAt);
    }

    [Fact]
    public void Withdrawing_without_a_pick_is_refused()
    {
        Assert.False(Investigating().TryPlay(Seat.A, new Withdraw()));
    }

    [Fact]
    public void No_lead_opens_during_the_final_answer()
    {
        var game = Investigating();
        game.TryPlay(Seat.A, new Accuse("s3"));
        game.TryPlay(Seat.B, new Accuse("s3"));

        Assert.False(game.TryPlay(Seat.A, new OpenLead("c1")));
    }

    [Fact]
    public void Who_opened_each_lead_is_kept()
    {
        var game = Investigating();

        game.TryPlay(Seat.B, new OpenLead("c1"));

        Assert.Equal([new OpenedLead(Seat.B, "c1")], game.State.Opened);
        Assert.Equal(Seat.B, game.View(Seat.A).Leads.Single(lead => lead.Id == "c1").By);
    }

    [Fact]
    public void The_case_mood_is_in_every_view()
    {
        var game = Investigating(@case: CaseFixture.Fair() with { Mood = CaseMood.Frost });

        Assert.Equal(CaseMood.Frost, game.View(Seat.A).Mood);
        Assert.Equal(CaseMood.Frost, game.View(Seat.B).Mood);
    }

    [Fact]
    public void The_room_tick_reports_a_game_clock_change()
    {
        var room = new RoomEngine();
        var game = Investigating();
        game.TryPlay(Seat.A, new Accuse("s3"));
        game.TryPlay(Seat.B, new Accuse("s3"));
        room.Restore(game);

        Assert.Equal(RoomChange.Game, room.Advance(0.05, T0));
        Assert.Equal(RoomChange.None, room.Advance(0.05, T0.AddSeconds(1)));
        Assert.Equal(RoomChange.Game, room.Advance(0.05, T0.AddSeconds(5)));

        Assert.Equal(MysteryPhase.Revealed, game.State.Phase);
    }
}
