namespace FriendsCorner.Core.Engines.Mystery;

public enum MysteryPhase
{
    Writing,
    Investigating,
    Accusing,
    Revealed,
    Failed,
}

public sealed record OpenedLead(Seat By, string Lead);

// The whole game as saved. Request counts the cases asked for, so a case
// written for an earlier request is never taken. EndsAt is the hard clock and
// LocksAt the final answer; each is set by the first tick that needs it.
public sealed record MysteryState(
    MysteryPhase Phase,
    int Request,
    IReadOnlyList<Seat> Playing,
    Case? Case,
    IReadOnlyList<OpenedLead> Opened,
    IReadOnlyDictionary<Seat, string> Picks,
    MysterySettings Settings,
    DateTimeOffset? EndsAt = null,
    DateTimeOffset? LocksAt = null,
    bool TimedOut = false)
{
    public const int EasyLeads = 8;
    public const int HardLeads = 6;

    public int Leads => Settings.Level == MysteryLevel.Hard ? HardLeads : EasyLeads;

    public bool IsRace => Settings.Mode == MysteryMode.Race;

    public bool IsOpen(string lead) => Opened.Any(opened => opened.Lead == lead);

    public bool IsOpenFor(Seat seat, string lead) =>
        Opened.Any(opened => opened.Lead == lead && (!IsRace || opened.By == seat));

    // Together spends one budget; in a race each seat spends its own.
    public int LeadsLeftFor(Seat seat) =>
        Leads - (IsRace ? Opened.Count(opened => opened.By == seat) : Opened.Count);

    // In a race a pick is final, and a pick that stands without a reveal was wrong.
    public bool IsOut(Seat seat) => IsRace && Picks.ContainsKey(seat) && Picks[seat] != Case?.Solution.Killer;
}
