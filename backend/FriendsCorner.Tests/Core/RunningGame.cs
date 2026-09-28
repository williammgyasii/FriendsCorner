namespace FriendsCorner.Tests;

internal static class RunningGame
{
    public static ChessBoard RunningChess(this RoomEngine room) => Assert.IsType<ChessGame>(room.Game).Board;

    public static Board RunningTicTacToe(this RoomEngine room) => Assert.IsType<TicTacToeGame>(room.Game).Board;
}
