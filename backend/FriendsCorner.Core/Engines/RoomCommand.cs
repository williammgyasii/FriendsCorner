using FriendsCorner.Core.Engines.Games;
using FriendsCorner.Core.Engines.Mystery;

namespace FriendsCorner.Core.Engines;

public abstract record RoomCommand;

public sealed record Steer(double X, double Y) : RoomCommand;

public sealed record Place(int Square) : GameMove;

public sealed record Rematch : RoomCommand;

public sealed record MoveChess(ChessMove Move) : GameMove;

public sealed record PickGame(string Game) : RoomCommand;

public sealed record SetReady(bool Ready) : RoomCommand;

public sealed record SetCapacity(int Size) : RoomCommand;

public sealed record ShareMedia(bool Camera, bool Mic) : RoomCommand;

public sealed record StartGame : RoomCommand;

public sealed record SetMysterySettings(MysterySettings Settings) : RoomCommand;

// What a command changed, so the caller knows what to show and what to save.
[Flags]
public enum RoomChange
{
    None = 0,
    Lobby = 1,
    Game = 2,
}
