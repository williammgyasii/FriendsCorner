namespace FriendsCorner.Tests;

public class MysteryViewTests
{
    private static MysteryGame MidInvestigation()
    {
        var game = MysteryGame.Start([Seat.A, Seat.B]);
        game.TryReceive(1, CaseFixture.Fair());
        game.TryPlay(Seat.A, new OpenLead("c1"));
        game.TryPlay(Seat.B, new OpenLead("t5"));
        game.TryPlay(Seat.A, new Accuse("s2"));
        return game;
    }

    [Fact]
    public void While_writing_the_view_has_only_the_phase_and_leads_left()
    {
        var view = MysteryGame.Start([Seat.A, Seat.B]).View(Seat.A);

        Assert.Equal(MysteryPhase.Writing, view.Phase);
        Assert.Null(view.Title);
        Assert.Empty(view.Suspects);
        Assert.Empty(view.Leads);
        Assert.Equal(8, view.LeadsLeft);
        Assert.Null(view.Outcome);
    }

    [Fact]
    public void Mid_investigation_only_open_leads_have_text()
    {
        var view = MidInvestigation().View(Seat.A);

        Assert.Equal(14, view.Leads.Count);
        Assert.Equal("The bread oven was lit at eleven and never left alone.", view.Leads.Single(lead => lead.Id == "c1").Text);
        Assert.NotNull(view.Leads.Single(lead => lead.Id == "t5").Text);
        Assert.All(view.Leads.Where(lead => lead.Id is not ("c1" or "t5")), lead => Assert.Null(lead.Text));
        Assert.Equal(6, view.LeadsLeft);
    }

    [Fact]
    public void Mid_investigation_the_view_shows_the_case_and_the_picks()
    {
        var view = MidInvestigation().View(Seat.A);

        Assert.Equal("Death at the Lantern Inn", view.Title);
        Assert.Equal(4, view.Suspects.Count);
        Assert.Equal(3, view.Places.Count);
        Assert.Equal("s2", view.Picks[Seat.A]);
        Assert.Null(view.Picks[Seat.B]);
    }

    [Fact]
    public void Before_the_reveal_the_view_holds_no_secret()
    {
        var view = MidInvestigation().View(Seat.A);

        Assert.Null(view.Outcome);
        Assert.DoesNotContain(typeof(Proof), PropertyTypes(typeof(MysteryView)));
        Assert.DoesNotContain(typeof(Solution), PropertyTypes(typeof(MysteryView)));
        Assert.DoesNotContain(nameof(Lead.Lie), typeof(LeadView).GetProperties().Select(property => property.Name));
    }

    [Fact]
    public void After_the_reveal_the_view_has_the_outcome()
    {
        var game = MidInvestigation();
        game.Agree("s3");

        var outcome = game.View(Seat.A).Outcome!;

        Assert.Equal("s3", outcome.Killer);
        Assert.True(outcome.Right);
        Assert.StartsWith("Clara followed Edmund", outcome.Story);
    }

    private static IEnumerable<Type> PropertyTypes(Type type) =>
        type.GetProperties().SelectMany(property =>
            property.PropertyType.IsGenericType
                ? property.PropertyType.GetGenericArguments().Prepend(property.PropertyType)
                : [property.PropertyType]);
}
