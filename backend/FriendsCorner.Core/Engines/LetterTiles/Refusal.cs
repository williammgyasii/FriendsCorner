namespace FriendsCorner.Core.Engines.LetterTiles;

public enum RefusalReason
{
    NotYourTurn,
    NotInRack,
    NotInLine,
    Gap,
    FirstMustCoverCentre,
    FirstNeedsTwoTiles,
    NotConnected,
    NotAWord,
    BagTooSmall,
    GameOver,
}

// Words is filled only for NotAWord.
public sealed record Refusal(RefusalReason Reason, IReadOnlyList<string> Words)
{
    public Refusal(RefusalReason reason)
        : this(reason, [])
    {
    }
}
