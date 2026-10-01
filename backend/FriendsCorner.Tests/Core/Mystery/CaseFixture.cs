namespace FriendsCorner.Tests;

// One fair case: s3 did it. c1+t2 prove s1, t3 proves s2, c5+t8 prove s4,
// and t5 is the killer's lie.
internal static class CaseFixture
{
    public static Case Fair() => new(
        Title: "Death at the Lantern Inn",
        Setting: "A snowed-in coaching inn on the moors, 1923.",
        Victim: new Victim("Edmund Hale", "Found at the foot of the cellar stairs at midnight."),
        Suspects:
        [
            new Suspect("s1", "Ada Finch", "Cook", "Edmund owed her wages.", "In the kitchen all evening."),
            new Suspect("s2", "Basil Crane", "Guest", "Edmund ruined his brother.", "Asleep in room 4."),
            new Suspect("s3", "Clara Voss", "Innkeeper", "Edmund was selling the inn.", "Doing the accounts."),
            new Suspect("s4", "Dev Mistry", "Stable hand", "Edmund had him dismissed.", "Tending the horses."),
        ],
        Places:
        [
            new Location("p1", "Kitchen"),
            new Location("p2", "Cellar"),
            new Location("p3", "Stables"),
        ],
        Leads:
        [
            Clue("c1", "p1", "The bread oven was lit at eleven and never left alone."),
            Clue("c2", "p1", "A carving knife is missing from the rack."),
            Clue("c3", "p2", "Candle wax on the third step, still soft."),
            Clue("c4", "p2", "A ledger page torn out and burned."),
            Clue("c5", "p3", "Fresh straw laid at half past eleven."),
            Clue("c6", "p3", "A lantern that belongs to the inn's office."),
            Said("t1", "s1", "I never left my oven."),
            Said("t2", "s1", "Dev came in for bread at quarter to twelve; I was kneading."),
            Said("t3", "s2", "The night porter locked me in at ten; he has the key."),
            Said("t4", "s2", "I heard footsteps on the cellar stairs."),
            Said("t5", "s3", "I didn't leave the office all night.", lie: true),
            Said("t6", "s3", "Edmund went down for wine."),
            Said("t7", "s4", "I was with the horses."),
            Said("t8", "s4", "Ada gave me bread and watched me eat it."),
        ],
        Proofs:
        [
            new Proof("s1", ["c1", "t2"]),
            new Proof("s2", ["t3"]),
            new Proof("s4", ["c5", "t8"]),
        ],
        Solution: new Solution(
            Killer: "s3",
            How: "Pushed him down the cellar stairs by lantern light.",
            Why: "To stop the sale of the inn.",
            Story: "Clara followed Edmund to the cellar with the office lantern, burned the page that proved the sale, and pushed him."));

    public static Lead Clue(string id, string place, string text) => new(id, LeadKind.Clue, place, text);

    public static Lead Said(string id, string suspect, string text, bool lie = false) =>
        new(id, LeadKind.Statement, suspect, text, lie);

    public static Case WithLead(this Case @case, Lead lead) =>
        @case with { Leads = @case.Leads.Select(old => old.Id == lead.Id ? lead : old).ToArray() };
}

internal static class MysteryPlay
{
    public static readonly DateTimeOffset T0 = new(2026, 9, 30, 20, 0, 0, TimeSpan.Zero);

    // Both accuse in together mode, then the final answer runs out.
    public static void Agree(this MysteryGame game, string suspect)
    {
        Assert.True(game.TryPlay(Seat.A, new Accuse(suspect)));
        Assert.True(game.TryPlay(Seat.B, new Accuse(suspect)));
        game.TryAdvance(T0);
        game.TryAdvance(T0 + MysteryGame.FinalAnswer);
    }
}
