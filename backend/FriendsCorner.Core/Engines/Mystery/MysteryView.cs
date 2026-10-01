namespace FriendsCorner.Core.Engines.Mystery;

// Text and By are null while the lead is closed, or not yours to see.
public sealed record LeadView(string Id, LeadKind Kind, string About, string? Text, Seat? By);

// Accused is null when nobody accused before the clock ran out. Winner is only set in a race.
public sealed record MysteryOutcome(
    string Killer,
    string How,
    string Why,
    string Story,
    string? Accused,
    bool Right,
    int Score,
    Seat? Winner,
    bool TimedOut);

// What one seat may see. It has no place for proofs, lie marks, or the
// solution; the solution only appears as the outcome after the reveal.
public sealed record MysteryView(
    MysteryPhase Phase,
    MysterySettings Settings,
    CaseMood? Mood,
    string? Title,
    string? Setting,
    Victim? Victim,
    IReadOnlyList<Suspect> Suspects,
    IReadOnlyList<Location> Places,
    IReadOnlyList<LeadView> Leads,
    int LeadsLeft,
    IReadOnlyDictionary<Seat, string?> Picks,
    IReadOnlyList<Seat> Out,
    DateTimeOffset? EndsAt,
    DateTimeOffset? LocksAt,
    MysteryOutcome? Outcome);
