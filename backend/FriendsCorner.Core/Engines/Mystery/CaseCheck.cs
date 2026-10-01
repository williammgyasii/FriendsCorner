namespace FriendsCorner.Core.Engines.Mystery;

public enum CaseVerdict
{
    Fair,
    Malformed,
    KillerNotASuspect,
    ProofClearsKiller,
    InnocentWithoutProof,
    TooManyLeadsNeeded,
    LiesMisplaced,
}

// Whether a written case can be played: its shape, then the fair-play rules.
// The writer is told the same rules, but this is the check the room trusts.
public static class CaseCheck
{
    public const int NameLength = 40;
    public const int TextLength = 400;
    public const int StoryLength = 1200;
    public const int MaxLeadsToSolve = 6;

    private static readonly string[] SuspectIds = ["s1", "s2", "s3", "s4"];
    private static readonly string[] PlaceIds = ["p1", "p2", "p3"];
    private static readonly string[] ClueIds = ["c1", "c2", "c3", "c4", "c5", "c6"];
    private static readonly string[] StatementIds = ["t1", "t2", "t3", "t4", "t5", "t6", "t7", "t8"];

    public static CaseVerdict Check(Case @case)
    {
        if (!IsWellFormed(@case))
        {
            return CaseVerdict.Malformed;
        }

        var killer = @case.Solution.Killer;
        if (!SuspectIds.Contains(killer))
        {
            return CaseVerdict.KillerNotASuspect;
        }

        if (@case.Proofs.Any(proof => proof.Suspect == killer))
        {
            return CaseVerdict.ProofClearsKiller;
        }

        var innocents = SuspectIds.Where(id => id != killer).ToArray();
        if (innocents.Any(id => @case.Proofs.All(proof => proof.Suspect != id)))
        {
            return CaseVerdict.InnocentWithoutProof;
        }

        if (!SolvableWithin(@case, innocents, MaxLeadsToSolve))
        {
            return CaseVerdict.TooManyLeadsNeeded;
        }

        var lies = @case.Leads.Where(lead => lead.Lie).ToArray();
        return lies is [{ Kind: LeadKind.Statement } lie] && lie.About == killer
            ? CaseVerdict.Fair
            : CaseVerdict.LiesMisplaced;
    }

    private static bool IsWellFormed(Case @case) =>
        HasText(@case.Title, TextLength) &&
        HasText(@case.Setting, TextLength) &&
        HasText(@case.Victim.Name, NameLength) &&
        HasText(@case.Victim.Found, TextLength) &&
        SameIds(@case.Suspects.Select(suspect => suspect.Id), SuspectIds) &&
        @case.Suspects.All(IsWellFormed) &&
        SameIds(@case.Places.Select(place => place.Id), PlaceIds) &&
        @case.Places.All(place => HasText(place.Name, NameLength)) &&
        LeadsAreWellFormed(@case.Leads) &&
        ProofsAreWellFormed(@case.Proofs) &&
        HasText(@case.Solution.Killer, NameLength) &&
        HasText(@case.Solution.How, TextLength) &&
        HasText(@case.Solution.Why, TextLength) &&
        HasText(@case.Solution.Story, StoryLength);

    private static bool IsWellFormed(Suspect suspect) =>
        HasText(suspect.Name, NameLength) &&
        HasText(suspect.Role, NameLength) &&
        HasText(suspect.Motive, TextLength) &&
        HasText(suspect.Alibi, TextLength);

    // Two clues at each place, two statements by each suspect, no lying clue.
    private static bool LeadsAreWellFormed(IReadOnlyList<Lead> leads)
    {
        var clues = leads.Where(lead => lead.Kind == LeadKind.Clue).ToArray();
        var statements = leads.Where(lead => lead.Kind == LeadKind.Statement).ToArray();
        return leads.Count == ClueIds.Length + StatementIds.Length &&
            SameIds(clues.Select(clue => clue.Id), ClueIds) &&
            SameIds(statements.Select(statement => statement.Id), StatementIds) &&
            leads.All(lead => HasText(lead.Text, TextLength)) &&
            clues.All(clue => !clue.Lie) &&
            PlaceIds.All(place => clues.Count(clue => clue.About == place) == 2) &&
            SuspectIds.All(suspect => statements.Count(statement => statement.About == suspect) == 2);
    }

    private static bool ProofsAreWellFormed(IReadOnlyList<Proof> proofs) =>
        proofs.Count is >= 1 and <= 12 &&
        proofs.All(proof =>
            SuspectIds.Contains(proof.Suspect) &&
            proof.Leads.Count is >= 1 and <= 3 &&
            proof.Leads.Distinct().Count() == proof.Leads.Count &&
            proof.Leads.All(lead => ClueIds.Contains(lead) || StatementIds.Contains(lead)));

    // Exact, not greedy: tries every set of at most `limit` of the 14 leads
    // (6,476 sets for a limit of 6) for one holding a whole proof per innocent.
    private static bool SolvableWithin(Case @case, IReadOnlyList<string> innocents, int limit)
    {
        var ids = @case.Leads.Select(lead => lead.Id).ToArray();
        var proofMasks = innocents
            .Select(innocent => @case.Proofs
                .Where(proof => proof.Suspect == innocent)
                .Select(proof => proof.Leads.Aggregate(0, (mask, lead) => mask | (1 << Array.IndexOf(ids, lead))))
                .ToArray())
            .ToArray();

        for (var open = 0; open < 1 << ids.Length; open++)
        {
            if (int.PopCount(open) <= limit &&
                proofMasks.All(masks => masks.Any(mask => (open & mask) == mask)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasText(string? text, int max) =>
        !string.IsNullOrWhiteSpace(text) && text.Length <= max;

    private static bool SameIds(IEnumerable<string> ids, IReadOnlyCollection<string> expected)
    {
        var list = ids.ToArray();
        return list.Length == expected.Count && list.ToHashSet().SetEquals(expected);
    }
}
