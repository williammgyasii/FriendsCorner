using System.Text.Json;
using DistanceTogether;

namespace DistanceTogether.Server;

public readonly record struct Applied(string? Forward, bool OpenedWorld)
{
    public bool ChangedBoard { get; init; }
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
}
