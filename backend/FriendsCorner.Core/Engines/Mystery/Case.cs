namespace FriendsCorner.Core.Engines.Mystery;

public sealed record Victim(string Name, string Found);

public sealed record Suspect(string Id, string Name, string Role, string Motive, string Alibi);

public sealed record Location(string Id, string Name);

public enum LeadKind
{
    Clue,
    Statement,
}

// About is a place id for a clue and a suspect id for a statement.
public sealed record Lead(string Id, LeadKind Kind, string About, string Text, bool Lie = false);

// These leads, once all open, clear the suspect. Players never see proofs.
public sealed record Proof(string Suspect, IReadOnlyList<string> Leads);

public sealed record Solution(string Killer, string How, string Why, string Story);

// How the case should look; the page picks colours and weather from it.
public enum CaseMood
{
    Frost,
    Storm,
    Velvet,
    Garden,
    Smoke,
    Gilded,
}

// Everything the writer produced. Only MysteryGame.View decides what a seat may see of it.
// Minutes is the writer's guess at how long a good pair needs; the game clamps it.
public sealed record Case(
    string Title,
    string Setting,
    Victim Victim,
    IReadOnlyList<Suspect> Suspects,
    IReadOnlyList<Location> Places,
    IReadOnlyList<Lead> Leads,
    IReadOnlyList<Proof> Proofs,
    Solution Solution,
    int Minutes = 12,
    CaseMood Mood = CaseMood.Smoke);
