using FriendsCorner.Core.Engines.Games;

namespace FriendsCorner.Core.Engines.Mystery;

// A game whose content arrives after it starts. The room asks for case
// WaitingFor at Level and hands it back; the number makes a late answer harmless.
public interface IAwaitsCase
{
    int? WaitingFor { get; }

    MysteryLevel Level { get; }

    bool TryReceive(int request, Case @case);

    bool TryFail(int request);
}

// Together: both players spend the same leads, and matching picks start a
// final answer that locks unless someone holds on. Race: each player spends
// their own leads and gets one guess; the first right guess wins. Hard adds a clock.
public sealed class MysteryGame(MysteryState state) : IGameEngine, IAwaitsCase
{
    public const string GameId = "mystery";
    public const int Players = 2;
    public const int SolvedScore = 100;
    public const int ScorePerLeadLeft = 10;
    public const int HardBonus = 50;
    public const int MinMinutes = 5;
    public const int MaxMinutes = 20;
    public static readonly TimeSpan FinalAnswer = TimeSpan.FromSeconds(5);

    public string Id => GameId;

    public MysteryState State { get; private set; } = state;

    public int? WaitingFor => State.Phase == MysteryPhase.Writing ? State.Request : null;

    public MysteryLevel Level => State.Settings.Level;

    private bool Playing => State.Phase is MysteryPhase.Investigating or MysteryPhase.Accusing;

    public static MysteryGame Start(IReadOnlyList<Seat> playing, MysterySettings? settings = null) =>
        new(Writing(playing, request: 1, settings ?? MysterySettings.Default));

    public static MysteryGame? TryStart(IReadOnlyList<Seat> playing, MysterySettings? settings = null) =>
        playing.Count == Players ? Start(playing, settings) : null;

    // The write that was in flight died with the process, so the players rematch.
    public static MysteryGame Restore(MysteryState saved) =>
        new(saved.Phase == MysteryPhase.Writing ? saved with { Phase = MysteryPhase.Failed } : saved);

    public bool TryReceive(int request, Case @case)
    {
        if (WaitingFor != request || CaseCheck.Check(@case) != CaseVerdict.Fair)
        {
            return false;
        }

        State = State with { Phase = MysteryPhase.Investigating, Case = @case };
        return true;
    }

    public bool TryFail(int request)
    {
        if (WaitingFor != request)
        {
            return false;
        }

        State = State with { Phase = MysteryPhase.Failed };
        return true;
    }

    public bool TryPlay(Seat seat, GameMove move)
    {
        if (!Playing || !State.Playing.Contains(seat) || State.IsOut(seat))
        {
            return false;
        }

        return move switch
        {
            OpenLead open => TryOpen(seat, open.Lead),
            Accuse accuse when State.IsRace => TryGuess(seat, accuse.Suspect),
            Accuse accuse => TryAccuse(seat, accuse.Suspect),
            Withdraw when !State.IsRace => TryWithdraw(seat),
            _ => false,
        };
    }

    // Starts whichever clock is waiting for its first tick, then lets the
    // clocks that are running end the case.
    public bool TryAdvance(DateTimeOffset now)
    {
        if (!Playing)
        {
            return false;
        }

        var changed = false;
        if (Level == MysteryLevel.Hard && State.EndsAt is null)
        {
            State = State with { EndsAt = now + TimeLimit(State.Case!) };
            changed = true;
        }
        else if (State.EndsAt is { } endsAt && now >= endsAt)
        {
            State = State with { Phase = MysteryPhase.Revealed, TimedOut = true, LocksAt = null };
            return true;
        }

        if (State.Phase == MysteryPhase.Accusing)
        {
            if (State.LocksAt is null)
            {
                State = State with { LocksAt = now + FinalAnswer };
                return true;
            }

            if (now >= State.LocksAt)
            {
                State = State with { Phase = MysteryPhase.Revealed };
                return true;
            }
        }

        return changed;
    }

    public bool TryRematch()
    {
        if (State.Phase is not (MysteryPhase.Revealed or MysteryPhase.Failed))
        {
            return false;
        }

        State = Writing(State.Playing, State.Request + 1, State.Settings);
        return true;
    }

    // Until the reveal, a race hides your partner's leads and pick from you.
    public MysteryView View(Seat you)
    {
        var secret = State.IsRace && State.Phase != MysteryPhase.Revealed;
        var picks = State.Playing.ToDictionary(
            seat => seat,
            seat => secret && seat != you ? null : State.Picks.GetValueOrDefault(seat));
        var leadsLeft = State.LeadsLeftFor(you);
        var @out = State.Playing.Where(State.IsOut).ToArray();
        if (State.Case is not { } @case)
        {
            return new MysteryView(State.Phase, State.Settings, null, null, null, null, [], [], [], leadsLeft, picks, @out, null, null, null);
        }

        var leads = @case.Leads
            .Select(lead => State.Opened.FirstOrDefault(opened => opened.Lead == lead.Id && (!secret || opened.By == you)) is { } opened
                ? new LeadView(lead.Id, lead.Kind, lead.About, lead.Text, opened.By)
                : new LeadView(lead.Id, lead.Kind, lead.About, null, null))
            .ToArray();
        return new MysteryView(
            State.Phase,
            State.Settings,
            @case.Mood,
            @case.Title,
            @case.Setting,
            @case.Victim,
            @case.Suspects,
            @case.Places,
            leads,
            leadsLeft,
            picks,
            @out,
            State.EndsAt,
            State.LocksAt,
            State.Phase == MysteryPhase.Revealed ? Outcome(@case) : null);
    }

    private static TimeSpan TimeLimit(Case @case) => TimeSpan.FromMinutes(Math.Clamp(@case.Minutes, MinMinutes, MaxMinutes));

    private bool TryOpen(Seat seat, string lead)
    {
        if (State.Phase != MysteryPhase.Investigating ||
            State.LeadsLeftFor(seat) == 0 ||
            State.IsOpenFor(seat, lead) ||
            State.Case!.Leads.All(known => known.Id != lead))
        {
            return false;
        }

        State = State with { Opened = [.. State.Opened, new OpenedLead(seat, lead)] };
        return true;
    }

    private bool TryAccuse(Seat seat, string suspect)
    {
        if (State.Case!.Suspects.All(known => known.Id != suspect))
        {
            return false;
        }

        var picks = new Dictionary<Seat, string>(State.Picks) { [seat] = suspect };
        var agreed = State.Playing.All(player => picks.GetValueOrDefault(player) == suspect);
        var stillLocking = agreed && State.Phase == MysteryPhase.Accusing;
        State = State with
        {
            Picks = picks,
            Phase = agreed ? MysteryPhase.Accusing : MysteryPhase.Investigating,
            LocksAt = stillLocking ? State.LocksAt : null,
        };
        return true;
    }

    // A race guess is final: right reveals at once, wrong puts you out, and
    // the game ends when nobody is left to guess.
    private bool TryGuess(Seat seat, string suspect)
    {
        if (State.Case!.Suspects.All(known => known.Id != suspect) || State.Picks.ContainsKey(seat))
        {
            return false;
        }

        State = State with { Picks = new Dictionary<Seat, string>(State.Picks) { [seat] = suspect } };
        if (suspect == State.Case.Solution.Killer || State.Playing.All(State.IsOut))
        {
            State = State with { Phase = MysteryPhase.Revealed };
        }

        return true;
    }

    private bool TryWithdraw(Seat seat)
    {
        if (!State.Picks.ContainsKey(seat))
        {
            return false;
        }

        var picks = new Dictionary<Seat, string>(State.Picks);
        picks.Remove(seat);
        State = State with { Picks = picks, Phase = MysteryPhase.Investigating, LocksAt = null };
        return true;
    }

    private MysteryOutcome Outcome(Case @case)
    {
        var solution = @case.Solution;
        if (State.TimedOut)
        {
            return new MysteryOutcome(solution.Killer, solution.How, solution.Why, solution.Story, null, false, 0, null, true);
        }

        if (State.IsRace)
        {
            Seat? winner = State.Playing.Where(seat => State.Picks.GetValueOrDefault(seat) == solution.Killer).Cast<Seat?>().FirstOrDefault();
            return winner is { } won
                ? new MysteryOutcome(solution.Killer, solution.How, solution.Why, solution.Story, solution.Killer, true, Score(won), won, false)
                : new MysteryOutcome(solution.Killer, solution.How, solution.Why, solution.Story, null, false, 0, null, false);
        }

        var first = State.Playing[0];
        var accused = State.Picks[first];
        var right = accused == solution.Killer;
        return new MysteryOutcome(solution.Killer, solution.How, solution.Why, solution.Story, accused, right, right ? Score(first) : 0, null, false);
    }

    private int Score(Seat seat) =>
        SolvedScore + (ScorePerLeadLeft * State.LeadsLeftFor(seat)) + (Level == MysteryLevel.Hard ? HardBonus : 0);

    private static MysteryState Writing(IReadOnlyList<Seat> playing, int request, MysterySettings settings) =>
        new(MysteryPhase.Writing, request, playing, null, [], new Dictionary<Seat, string>(), settings);
}
