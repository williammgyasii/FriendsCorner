namespace FriendsCorner.Tests;

// Race: own leads, one final guess each, first right guess wins. s3 did it.
public class MysteryRaceTests
{
    private static readonly DateTimeOffset T0 = MysteryPlay.T0;

    private static MysteryGame Race(MysteryLevel level = MysteryLevel.Easy, int minutes = 12)
    {
        var game = MysteryGame.Start([Seat.A, Seat.B], new MysterySettings(level, MysteryMode.Race));
        Assert.True(game.TryReceive(1, CaseFixture.Fair() with { Minutes = minutes }));
        return game;
    }

    [Fact]
    public void Each_player_spends_their_own_leads()
    {
        var game = Race();

        game.TryPlay(Seat.A, new OpenLead("c1"));

        Assert.Equal(7, game.State.LeadsLeftFor(Seat.A));
        Assert.Equal(8, game.State.LeadsLeftFor(Seat.B));
    }

    [Fact]
    public void Both_players_may_open_the_same_lead()
    {
        var game = Race();
        game.TryPlay(Seat.A, new OpenLead("c1"));

        Assert.True(game.TryPlay(Seat.B, new OpenLead("c1")));
        Assert.False(game.TryPlay(Seat.B, new OpenLead("c1")));
    }

    [Fact]
    public void A_ninth_lead_of_your_own_is_refused()
    {
        var game = Race();
        foreach (var lead in new[] { "c1", "c2", "c3", "c4", "c5", "c6", "t1", "t2" })
        {
            Assert.True(game.TryPlay(Seat.A, new OpenLead(lead)));
        }

        Assert.False(game.TryPlay(Seat.A, new OpenLead("t3")));
        Assert.True(game.TryPlay(Seat.B, new OpenLead("t3")));
    }

    [Fact]
    public void Your_partners_leads_and_pick_stay_hidden()
    {
        var game = Race();
        game.TryPlay(Seat.A, new OpenLead("c1"));
        game.TryPlay(Seat.A, new Accuse("s1"));

        var seenByB = game.View(Seat.B);

        var c1 = seenByB.Leads.Single(lead => lead.Id == "c1");
        Assert.Null(c1.Text);
        Assert.Null(c1.By);
        Assert.Null(seenByB.Picks[Seat.A]);
        Assert.Equal(8, seenByB.LeadsLeft);
        Assert.NotNull(game.View(Seat.A).Leads.Single(lead => lead.Id == "c1").Text);
    }

    [Fact]
    public void A_right_accusation_wins_at_once()
    {
        var game = Race();
        foreach (var lead in new[] { "c1", "t2", "t3" })
        {
            game.TryPlay(Seat.B, new OpenLead(lead));
        }

        Assert.True(game.TryPlay(Seat.B, new Accuse("s3")));

        Assert.Equal(MysteryPhase.Revealed, game.State.Phase);
        var outcome = game.View(Seat.A).Outcome!;
        Assert.Equal(Seat.B, outcome.Winner);
        Assert.True(outcome.Right);
        Assert.Equal("s3", outcome.Accused);
        Assert.Equal(100 + 50, outcome.Score);
    }

    [Fact]
    public void A_hard_race_win_adds_the_bonus()
    {
        var game = Race(MysteryLevel.Hard);

        game.TryPlay(Seat.A, new Accuse("s3"));

        Assert.Equal(100 + 60 + 50, game.View(Seat.A).Outcome!.Score);
    }

    [Fact]
    public void A_wrong_accusation_puts_you_out()
    {
        var game = Race();

        Assert.True(game.TryPlay(Seat.A, new Accuse("s1")));

        Assert.Equal(MysteryPhase.Investigating, game.State.Phase);
        Assert.False(game.TryPlay(Seat.A, new OpenLead("c1")));
        Assert.False(game.TryPlay(Seat.A, new Accuse("s3")));
        Assert.False(game.TryPlay(Seat.A, new Withdraw()));
        Assert.Equal([Seat.A], game.View(Seat.B).Out);
        Assert.Equal([Seat.A], game.View(Seat.A).Out);
    }

    [Fact]
    public void Your_partner_can_still_win_after_you_are_out()
    {
        var game = Race();
        game.TryPlay(Seat.A, new Accuse("s1"));

        Assert.True(game.TryPlay(Seat.B, new Accuse("s3")));

        Assert.Equal(Seat.B, game.View(Seat.A).Outcome!.Winner);
    }

    [Fact]
    public void Both_out_reveals_with_no_winner()
    {
        var game = Race();

        game.TryPlay(Seat.A, new Accuse("s1"));
        game.TryPlay(Seat.B, new Accuse("s2"));

        Assert.Equal(MysteryPhase.Revealed, game.State.Phase);
        var outcome = game.View(Seat.A).Outcome!;
        Assert.Null(outcome.Winner);
        Assert.False(outcome.Right);
        Assert.Equal(0, outcome.Score);
        Assert.False(outcome.TimedOut);
    }

    [Fact]
    public void Running_out_of_time_reveals_with_no_winner()
    {
        var game = Race(MysteryLevel.Hard, minutes: 8);
        game.TryAdvance(T0);

        Assert.True(game.TryAdvance(T0.AddMinutes(8)));

        var outcome = game.View(Seat.B).Outcome!;
        Assert.True(outcome.TimedOut);
        Assert.Null(outcome.Winner);
        Assert.Equal(0, outcome.Score);
    }

    [Fact]
    public void After_the_reveal_both_see_everything()
    {
        var game = Race();
        game.TryPlay(Seat.A, new OpenLead("c1"));
        game.TryPlay(Seat.A, new Accuse("s1"));
        game.TryPlay(Seat.B, new Accuse("s2"));

        var seenByB = game.View(Seat.B);

        Assert.Equal("s1", seenByB.Picks[Seat.A]);
        Assert.NotNull(seenByB.Leads.Single(lead => lead.Id == "c1").Text);
    }

    [Fact]
    public void A_race_never_enters_the_final_answer()
    {
        var game = Race();
        game.TryPlay(Seat.A, new Accuse("s1"));

        Assert.False(game.TryAdvance(T0));
        Assert.Null(game.State.LocksAt);
    }
}
