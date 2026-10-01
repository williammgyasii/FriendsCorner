namespace FriendsCorner.Infrastructure.Persistence;

// The whole game, case included, as one jsonb document: it is always read
// and written whole, never queried by field.
public sealed class MysteryRow
{
    public required string RoomId { get; set; }
    public required string State { get; set; }
}
