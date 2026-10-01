namespace FriendsCorner.Core.Engines.Mystery;

public enum MysteryLevel
{
    Easy,
    Hard,
}

public enum MysteryMode
{
    Together,
    Race,
}

// Chosen in the lobby before the game exists, and kept across rematches.
public sealed record MysterySettings(MysteryLevel Level, MysteryMode Mode)
{
    public static readonly MysterySettings Default = new(MysteryLevel.Easy, MysteryMode.Together);
}
