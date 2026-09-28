namespace FriendsCorner.Infrastructure.Persistence;

public sealed class ChessRow
{
    public required string RoomId { get; set; }
    public required string Fen { get; set; }
    public required string WhiteSeat { get; set; }
}
