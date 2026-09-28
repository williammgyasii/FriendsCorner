namespace FriendsCorner.Infrastructure.Persistence;

public sealed class TicTacToeRow
{
    public required string RoomId { get; set; }
    public required string Squares { get; set; }
    public required string NextSeat { get; set; }
}
