namespace FriendsCorner.Core.Engines;

public abstract record RoomCommand;

public sealed record Steer(double X, double Y) : RoomCommand;

public sealed record Place(int Square) : RoomCommand;

public sealed record Rematch : RoomCommand;

public sealed record MoveChess(ChessMove Move) : RoomCommand;

public sealed record ChessRematch : RoomCommand;

public sealed record PickGame(string Game) : RoomCommand;

public sealed record SetReady(bool Ready) : RoomCommand;

public sealed record SetCapacity(int Size) : RoomCommand;

public sealed record ShareMedia(bool Camera, bool Mic) : RoomCommand;

public sealed record StartGame : RoomCommand;

// What a command changed, so the caller knows what to show and what to save.
[Flags]
public enum RoomChange
{
    None = 0,
    Lobby = 1,
    TicTacToe = 2,
    Chess = 4,
}
