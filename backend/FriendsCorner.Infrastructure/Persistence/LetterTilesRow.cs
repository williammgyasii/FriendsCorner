namespace FriendsCorner.Infrastructure.Persistence;

// The whole game as one jsonb document: it is always read and written whole,
// never queried by field.
public sealed class LetterTilesRow
{
    public required string RoomId { get; set; }
    public required string State { get; set; }
}
