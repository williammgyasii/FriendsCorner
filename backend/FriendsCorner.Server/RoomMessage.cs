using System.Text.Json;
using FriendsCorner;

namespace FriendsCorner.Server;

public readonly record struct Applied(string? Forward, bool OpenedWorld)
{
    public bool ChangedBoard { get; init; }

    public bool ChangedChess { get; init; }
}

public static class RoomMessage
{
    public static Applied Apply(Room room, Seat seat, string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var typeElement))
            {
                return default;
            }

            var type = typeElement.GetString();
            if (type == "signal")
            {
                return new Applied(json, false);
            }

            if (type == "launch")
            {
                if (!root.TryGetProperty("world", out var worldElement))
                {
                    return default;
                }

                var world = worldElement.GetString();
                if (world is null)
                {
                    return default;
                }

                var opened = room.TryLaunch(world);
                return new Applied(null, opened)
                {
                    ChangedBoard = opened && world == "tictactoe",
                    ChangedChess = opened && world == "chess",
                };
            }

            if (type == "chess-move")
            {
                return new Applied(null, false)
                {
                    ChangedChess = TryReadChessMove(root, out var move) && room.TryChessMove(seat, move),
                };
            }

            if (type == "chess-rematch")
            {
                return new Applied(null, false)
                {
                    ChangedChess = room.TryChessRematch(),
                };
            }

            if (type == "rematch")
            {
                return new Applied(null, false)
                {
                    ChangedBoard = room.TryRematch(),
                };
            }

            if (type == "place")
            {
                if (!root.TryGetProperty("square", out var squareElement) || !squareElement.TryGetInt32(out var square))
                {
                    return default;
                }

                return new Applied(null, false)
                {
                    ChangedBoard = room.TryPlace(seat, square),
                };
            }

            if (type != "direction")
            {
                return default;
            }

            if (!root.TryGetProperty("x", out var xElement) || !root.TryGetProperty("y", out var yElement))
            {
                return default;
            }

            if (!xElement.TryGetDouble(out var x) || !yElement.TryGetDouble(out var y))
            {
                return default;
            }

            room.TrySetDirection(seat, x, y);
            return default;
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static bool TryReadChessMove(JsonElement root, out ChessMove move)
    {
        move = default;
        if (!root.TryGetProperty("from", out var fromElement) || fromElement.ValueKind != JsonValueKind.String ||
            !root.TryGetProperty("to", out var toElement) || toElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        char? promotion = null;
        if (root.TryGetProperty("promotion", out var promotionElement) &&
            promotionElement.ValueKind == JsonValueKind.String &&
            promotionElement.GetString() is { Length: 1 } piece)
        {
            promotion = piece[0];
        }

        move = new ChessMove(fromElement.GetString()!, toElement.GetString()!, promotion);
        return true;
    }
}
