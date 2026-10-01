using System.Text.Json;
using System.Text.Json.Serialization;
using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;
using FriendsCorner.Core.Engines.Mystery;
using FriendsCorner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FriendsCorner.Infrastructure.Accessors;

public sealed class MysteryTableAccessor(IDbContextFactory<FriendsCornerDb> contexts) : IMysteryTableAccessor
{
    // Pinned here, not left to defaults, so what is on disk only changes on purpose.
    private static readonly JsonSerializerOptions Stored = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    // Find-then-write is safe because each room's recorder is its only writer.
    public async Task Save(string roomId, MysteryState state)
    {
        var json = JsonSerializer.Serialize(StoredMystery.From(state), Stored);
        await using var db = await contexts.CreateDbContextAsync();
        var row = await db.Mystery.FindAsync(roomId);
        if (row is null)
        {
            db.Mystery.Add(new MysteryRow { RoomId = roomId, State = json });
        }
        else
        {
            row.State = json;
        }

        await db.SaveChangesAsync();
    }

    public async Task<MysteryState?> Load(string roomId)
    {
        await using var db = await contexts.CreateDbContextAsync();
        var row = await db.Mystery.AsNoTracking().SingleOrDefaultAsync(r => r.RoomId == roomId);
        return row is null ? null : Read(row.State);
    }

    // A row in a shape this build can't read starts the room fresh.
    private static MysteryState? Read(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<StoredMystery>(json, Stored)?.ToState();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task Remove(string roomId)
    {
        await using var db = await contexts.CreateDbContextAsync();
        await db.Mystery.Where(r => r.RoomId == roomId).ExecuteDeleteAsync();
    }

    // Seats go to disk as names. The case keeps the shape the writer's
    // schema promises, so a saved case reads like a written one.
    private sealed record StoredMystery(
        MysteryPhase Phase,
        int Request,
        string[] Playing,
        Case? Case,
        StoredLead[] Opened,
        Dictionary<string, string> Picks,
        MysterySettings Settings,
        DateTimeOffset? EndsAt,
        DateTimeOffset? LocksAt,
        bool TimedOut)
    {
        public static StoredMystery From(MysteryState state) => new(
            state.Phase,
            state.Request,
            state.Playing.Select(seat => seat.ToString()).ToArray(),
            state.Case,
            state.Opened.Select(opened => new StoredLead(opened.By.ToString(), opened.Lead)).ToArray(),
            state.Picks.ToDictionary(pick => pick.Key.ToString(), pick => pick.Value),
            state.Settings,
            state.EndsAt,
            state.LocksAt,
            state.TimedOut);

        public MysteryState ToState() => new(
            Phase,
            Request,
            Playing.Select(Enum.Parse<Seat>).ToArray(),
            Case,
            Opened.Select(opened => new OpenedLead(Enum.Parse<Seat>(opened.By), opened.Lead)).ToArray(),
            Picks.ToDictionary(pick => Enum.Parse<Seat>(pick.Key), pick => pick.Value),
            Settings,
            EndsAt,
            LocksAt,
            TimedOut);
    }

    private sealed record StoredLead(string By, string Lead);
}
