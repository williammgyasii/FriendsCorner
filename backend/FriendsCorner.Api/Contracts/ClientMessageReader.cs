using System.Text.Json;
using FriendsCorner.Core.Engines;

namespace FriendsCorner.Api.Contracts;

public interface IClientMessageReader
{
    ClientMessage? Read(string json);
}

// A text frame from a browser, read into what the room should do with it.
// Anything malformed reads as null and is ignored.
public sealed class ClientMessageReader : IClientMessageReader
{
    public ClientMessage? Read(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || Text(root, "type") is not { } type)
            {
                return null;
            }

            if (type == "signal")
            {
                return new Relay(json);
            }

            return Command(type, root) is { } command ? new Act(command) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static RoomCommand? Command(string type, JsonElement root) => type switch
    {
        "direction" when Number(root, "x") is { } x && Number(root, "y") is { } y => new Steer(x, y),
        "place" when Whole(root, "square") is { } square => new Place(square),
        "rematch" => new Rematch(),
        "chess-move" when Text(root, "from") is { } from && Text(root, "to") is { } to =>
            new MoveChess(new ChessMove(from, to, Text(root, "promotion") is [var piece] ? piece : null)),
        "chess-rematch" => new ChessRematch(),
        "pick" when Text(root, "game") is { } game => new PickGame(game),
        "ready" when Flag(root, "ready") is { } ready => new SetReady(ready),
        "capacity" when Whole(root, "size") is { } size => new SetCapacity(size),
        "media" when Flag(root, "camera") is { } camera && Flag(root, "mic") is { } mic => new ShareMedia(camera, mic),
        "start" => new StartGame(),
        _ => null,
    };

    private static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static double? Number(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number)
            ? number
            : null;

    private static int? Whole(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
            ? number
            : null;

    private static bool? Flag(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;
}
