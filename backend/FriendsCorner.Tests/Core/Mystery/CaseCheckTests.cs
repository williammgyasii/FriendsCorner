namespace FriendsCorner.Tests;

public class CaseCheckTests
{
    private static readonly Case Fair = CaseFixture.Fair();

    [Fact]
    public void The_fixture_case_is_fair()
    {
        Assert.Equal(CaseVerdict.Fair, CaseCheck.Check(Fair));
    }

    [Fact]
    public void Seven_statements_is_malformed()
    {
        var @case = Fair with { Leads = Fair.Leads.Where(lead => lead.Id != "t8").ToArray() };

        Assert.Equal(CaseVerdict.Malformed, CaseCheck.Check(@case));
    }

    [Fact]
    public void Three_suspects_is_malformed()
    {
        var @case = Fair with { Suspects = Fair.Suspects.Take(3).ToArray() };

        Assert.Equal(CaseVerdict.Malformed, CaseCheck.Check(@case));
    }

    [Fact]
    public void A_clue_at_an_unknown_place_is_malformed()
    {
        var @case = Fair.WithLead(CaseFixture.Clue("c2", "p9", "A carving knife is missing."));

        Assert.Equal(CaseVerdict.Malformed, CaseCheck.Check(@case));
    }

    [Fact]
    public void A_401_character_clue_is_malformed()
    {
        var @case = Fair.WithLead(CaseFixture.Clue("c2", "p1", new string('x', 401)));

        Assert.Equal(CaseVerdict.Malformed, CaseCheck.Check(@case));
    }

    [Fact]
    public void A_400_character_clue_is_fine()
    {
        var @case = Fair.WithLead(CaseFixture.Clue("c2", "p1", new string('x', 400)));

        Assert.Equal(CaseVerdict.Fair, CaseCheck.Check(@case));
    }

    [Fact]
    public void A_proof_naming_an_unknown_lead_is_malformed()
    {
        var @case = Fair with { Proofs = [.. Fair.Proofs, new Proof("s2", ["c9"])] };

        Assert.Equal(CaseVerdict.Malformed, CaseCheck.Check(@case));
    }

    [Fact]
    public void A_lie_on_a_clue_is_malformed()
    {
        var @case = Fair.WithLead(Fair.Leads.Single(lead => lead.Id == "c2") with { Lie = true });

        Assert.Equal(CaseVerdict.Malformed, CaseCheck.Check(@case));
    }

    [Fact]
    public void A_killer_who_is_not_a_suspect_is_unfair()
    {
        var @case = Fair with { Solution = Fair.Solution with { Killer = "s9" } };

        Assert.Equal(CaseVerdict.KillerNotASuspect, CaseCheck.Check(@case));
    }

    [Fact]
    public void A_proof_that_clears_the_killer_is_unfair()
    {
        var @case = Fair with { Proofs = [.. Fair.Proofs, new Proof("s3", ["c4"])] };

        Assert.Equal(CaseVerdict.ProofClearsKiller, CaseCheck.Check(@case));
    }

    [Fact]
    public void An_innocent_with_no_proof_is_unfair()
    {
        var @case = Fair with { Proofs = Fair.Proofs.Where(proof => proof.Suspect != "s2").ToArray() };

        Assert.Equal(CaseVerdict.InnocentWithoutProof, CaseCheck.Check(@case));
    }

    [Fact]
    public void Needing_nine_leads_to_clear_everyone_is_unfair()
    {
        var @case = Fair with
        {
            Proofs =
            [
                new Proof("s1", ["c1", "c2", "t1"]),
                new Proof("s2", ["c3", "t3", "t4"]),
                new Proof("s4", ["c5", "c6", "t8"]),
            ],
        };

        Assert.Equal(CaseVerdict.TooManyLeadsNeeded, CaseCheck.Check(@case));
    }

    [Fact]
    public void Proofs_that_share_leads_can_fit_in_six()
    {
        var @case = Fair with
        {
            Proofs =
            [
                new Proof("s1", ["c1", "c2", "t1"]),
                new Proof("s2", ["c1", "c2", "t3"]),
                new Proof("s4", ["c1", "c5", "t8"]),
            ],
        };

        Assert.Equal(CaseVerdict.Fair, CaseCheck.Check(@case));
    }

    [Fact]
    public void A_killer_with_no_lie_is_unfair()
    {
        var @case = Fair.WithLead(CaseFixture.Said("t5", "s3", "I didn't leave the office all night."));

        Assert.Equal(CaseVerdict.LiesMisplaced, CaseCheck.Check(@case));
    }

    [Fact]
    public void An_innocent_who_lies_is_unfair()
    {
        var @case = Fair.WithLead(CaseFixture.Said("t7", "s4", "I was with the horses.", lie: true));

        Assert.Equal(CaseVerdict.LiesMisplaced, CaseCheck.Check(@case));
    }
}
