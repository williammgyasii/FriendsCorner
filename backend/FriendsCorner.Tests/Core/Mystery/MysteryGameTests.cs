namespace FriendsCorner.Tests;

public class MysteryGameTests
{
    private static readonly Case Fair = CaseFixture.Fair();

    private static MysteryGame Investigating()
    {
        var game = MysteryGame.Start([Seat.A, Seat.B]);
        Assert.True(game.TryReceive(1, Fair));
        return game;
    }

    private static void Open(MysteryGame game, params string[] leads)
    {
        foreach (var lead in leads)
        {
            Assert.True(game.TryPlay(Seat.A, new OpenLead(lead)));
        }
    }

    [Fact]
    public void A_new_game_is_writing_case_one_with_eight_leads()
    {
        var game = MysteryGame.Start([Seat.A, Seat.B]);

        Assert.Equal(MysteryGame.GameId, game.Id);
        Assert.Equal(MysteryPhase.Writing, game.State.Phase);
        Assert.Equal(1, game.WaitingFor);
        Assert.Equal(8, game.State.LeadsLeftFor(Seat.A));
    }

    [Fact]
    public void Only_two_players_can_start()
    {
        Assert.Null(MysteryGame.TryStart([Seat.A]));
        Assert.Null(MysteryGame.TryStart([Seat.A, Seat.B, Seat.C]));
        Assert.NotNull(MysteryGame.TryStart([Seat.A, Seat.B]));
    }

    [Fact]
    public void The_awaited_case_starts_the_investigation()
    {
        var game = Investigating();

        Assert.Equal(MysteryPhase.Investigating, game.State.Phase);
        Assert.Null(game.WaitingFor);
        Assert.Same(Fair, game.State.Case);
    }

    [Fact]
    public void A_case_for_another_request_or_a_second_case_is_refused()
    {
        var game = MysteryGame.Start([Seat.A, Seat.B]);

        Assert.False(game.TryReceive(2, Fair));
        Assert.True(game.TryReceive(1, Fair));
        Assert.False(game.TryReceive(1, Fair));
    }

    [Fact]
    public void An_unfair_case_is_refused_even_if_awaited()
    {
        var game = MysteryGame.Start([Seat.A, Seat.B]);
        var unfair = Fair with { Proofs = [.. Fair.Proofs, new Proof("s3", ["c4"])] };

        Assert.False(game.TryReceive(1, unfair));
        Assert.Equal(MysteryPhase.Writing, game.State.Phase);
    }

    [Fact]
    public void Failing_the_awaited_request_ends_in_failed()
    {
        var game = MysteryGame.Start([Seat.A, Seat.B]);

        Assert.False(game.TryFail(2));
        Assert.True(game.TryFail(1));

        Assert.Equal(MysteryPhase.Failed, game.State.Phase);
        Assert.Null(game.WaitingFor);
    }

    [Fact]
    public void Opening_a_clue_spends_a_lead_for_both()
    {
        var game = Investigating();

        Assert.True(game.TryPlay(Seat.A, new OpenLead("c1")));

        Assert.True(game.State.IsOpen("c1"));
        Assert.Equal(7, game.State.LeadsLeftFor(Seat.A));
    }

    [Fact]
    public void Opening_the_same_lead_twice_is_refused()
    {
        var game = Investigating();
        Open(game, "c1");

        Assert.False(game.TryPlay(Seat.B, new OpenLead("c1")));
        Assert.Equal(7, game.State.LeadsLeftFor(Seat.A));
    }

    [Fact]
    public void An_unknown_lead_is_refused()
    {
        var game = Investigating();

        Assert.False(game.TryPlay(Seat.A, new OpenLead("c9")));
        Assert.Equal(8, game.State.LeadsLeftFor(Seat.A));
    }

    [Fact]
    public void A_ninth_lead_is_refused()
    {
        var game = Investigating();
        Open(game, "c1", "c2", "c3", "c4", "c5", "c6", "t1", "t2");

        Assert.False(game.TryPlay(Seat.A, new OpenLead("t5")));
        Assert.False(game.State.IsOpen("t5"));
        Assert.Equal(0, game.State.LeadsLeftFor(Seat.A));
    }

    [Fact]
    public void Leads_cannot_be_opened_while_writing()
    {
        var game = MysteryGame.Start([Seat.A, Seat.B]);

        Assert.False(game.TryPlay(Seat.A, new OpenLead("c1")));
    }

    [Fact]
    public void A_seat_that_is_not_playing_is_refused()
    {
        var game = Investigating();

        Assert.False(game.TryPlay(Seat.C, new OpenLead("c1")));
        Assert.False(game.TryPlay(Seat.C, new Accuse("s3")));
    }

    [Fact]
    public void Different_picks_do_not_reveal()
    {
        var game = Investigating();

        Assert.True(game.TryPlay(Seat.A, new Accuse("s2")));
        Assert.True(game.TryPlay(Seat.B, new Accuse("s3")));

        Assert.Equal(MysteryPhase.Investigating, game.State.Phase);
        Assert.Equal("s2", game.State.Picks[Seat.A]);
        Assert.Equal("s3", game.State.Picks[Seat.B]);
    }

    [Fact]
    public void The_same_pick_then_the_final_answer_reveals()
    {
        var game = Investigating();

        game.Agree("s3");

        Assert.Equal(MysteryPhase.Revealed, game.State.Phase);
    }

    [Fact]
    public void Changing_a_pick_to_match_starts_the_final_answer()
    {
        var game = Investigating();
        game.TryPlay(Seat.A, new Accuse("s2"));
        game.TryPlay(Seat.B, new Accuse("s3"));

        Assert.True(game.TryPlay(Seat.A, new Accuse("s3")));

        Assert.Equal(MysteryPhase.Accusing, game.State.Phase);
    }

    [Fact]
    public void An_unknown_suspect_is_refused()
    {
        var game = Investigating();

        Assert.False(game.TryPlay(Seat.A, new Accuse("s9")));
        Assert.Empty(game.State.Picks);
    }

    [Fact]
    public void Right_with_three_leads_left_scores_130()
    {
        var game = Investigating();
        Open(game, "c1", "c2", "c3", "t2", "t3");

        game.Agree("s3");

        var outcome = game.View(Seat.A).Outcome!;
        Assert.True(outcome.Right);
        Assert.Equal(130, outcome.Score);
        Assert.Equal("s3", outcome.Killer);
    }

    [Fact]
    public void Wrong_scores_zero_and_names_the_killer()
    {
        var game = Investigating();

        game.Agree("s1");

        var outcome = game.View(Seat.A).Outcome!;
        Assert.False(outcome.Right);
        Assert.Equal(0, outcome.Score);
        Assert.Equal("s3", outcome.Killer);
        Assert.Equal("s1", outcome.Accused);
    }

    [Fact]
    public void After_the_reveal_nothing_more_can_be_opened_or_picked()
    {
        var game = Investigating();
        game.Agree("s3");

        Assert.False(game.TryPlay(Seat.A, new OpenLead("c1")));
        Assert.False(game.TryPlay(Seat.A, new Accuse("s1")));
    }

    [Fact]
    public void Rematch_is_refused_while_writing_and_investigating()
    {
        Assert.False(MysteryGame.Start([Seat.A, Seat.B]).TryRematch());

        var game = Investigating();
        Open(game, "c1", "c2", "c3");
        Assert.False(game.TryRematch());
        Assert.Equal(5, game.State.LeadsLeftFor(Seat.A));
    }

    [Fact]
    public void Rematch_after_the_reveal_writes_case_two_from_scratch()
    {
        var game = Investigating();
        Open(game, "c1");
        game.Agree("s3");

        Assert.True(game.TryRematch());

        Assert.Equal(MysteryPhase.Writing, game.State.Phase);
        Assert.Equal(2, game.WaitingFor);
        Assert.Equal(8, game.State.LeadsLeftFor(Seat.A));
        Assert.Empty(game.State.Picks);
        Assert.Null(game.State.Case);
    }

    [Fact]
    public void Rematch_after_a_failure_writes_again()
    {
        var game = MysteryGame.Start([Seat.A, Seat.B]);
        game.TryFail(1);

        Assert.True(game.TryRematch());

        Assert.Equal(2, game.WaitingFor);
    }

    [Fact]
    public void A_game_saved_while_writing_comes_back_failed()
    {
        var saved = MysteryGame.Start([Seat.A, Seat.B]).State;

        var restored = MysteryGame.Restore(saved);

        Assert.Equal(MysteryPhase.Failed, restored.State.Phase);
        Assert.Null(restored.WaitingFor);
    }

    [Fact]
    public void A_game_saved_mid_investigation_comes_back_as_it_was()
    {
        var game = Investigating();
        Open(game, "c1", "t4");
        game.TryPlay(Seat.A, new Accuse("s2"));

        var restored = MysteryGame.Restore(game.State);

        Assert.Equal(game.State, restored.State);
    }
}
