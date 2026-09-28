using System.Text.Json;
using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;
using FriendsCorner.Core.Engines.LetterTiles;
using FriendsCorner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FriendsCorner.Infrastructure.Accessors;

public sealed class LetterTilesTableAccessor(IDbContextFactory<FriendsCornerDb> contexts) : ILetterTilesTableAccessor
{
    // Find-then-write is safe because each room's recorder is its only writer.
    public async Task Save(string roomId, LetterTilesState state)
    {
        var json = JsonSerializer.Serialize(StoredTiles.From(state));
        await using var db = await contexts.CreateDbContextAsync();
        var row = await db.LetterTiles.FindAsync(roomId);
        if (row is null)
        {
            db.LetterTiles.Add(new LetterTilesRow { RoomId = roomId, State = json });
        }
        else
        {
            row.State = json;
        }

        await db.SaveChangesAsync();
    }

    public async Task<LetterTilesState?> Load(string roomId)
    {
        await using var db = await contexts.CreateDbContextAsync();
        var row = await db.LetterTiles.AsNoTracking().SingleOrDefaultAsync(r => r.RoomId == roomId);
        return row is null ? null : JsonSerializer.Deserialize<StoredTiles>(row.State)?.ToState();
    }

    public async Task Remove(string roomId)
    {
        await using var db = await contexts.CreateDbContextAsync();
        await db.LetterTiles.Where(r => r.RoomId == roomId).ExecuteDeleteAsync();
    }

    // The stored shape, kept apart from the engine's record so a rules
    // refactor does not silently change what is on disk. Tiles are letters:
    // '.' is an empty square, lower case is a played blank, '?' an unplayed one.
    private sealed record StoredTiles(
        string Board,
        string Bag,
        Dictionary<string, string> Racks,
        Dictionary<string, int> Scores,
        string[] Playing,
        string ToMove,
        string Starter,
        int ScorelessTurns,
        StoredLastPlay? LastPlay,
        string[]? Winners)
    {
        private const char Empty = '.';

        public static StoredTiles From(LetterTilesState state) => new(
            string.Concat(state.Board.Select(tile => tile is { } placed ? Letter(placed) : Empty)),
            Letters(state.Bag),
            state.Racks.ToDictionary(pair => pair.Key.ToString(), pair => Letters(pair.Value)),
            state.Scores.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value),
            state.Playing.Select(seat => seat.ToString()).ToArray(),
            state.ToMove.ToString(),
            state.Starter.ToString(),
            state.ScorelessTurns,
            state.LastPlay is { } last ? new StoredLastPlay(last.Seat.ToString(), last.Words.ToArray(), last.Score) : null,
            state.Outcome?.Winners.Select(seat => seat.ToString()).ToArray());

        public LetterTilesState ToState() => new(
            Board.Select(letter => letter == Empty ? (Tile?)null : TileOf(letter)).ToArray(),
            Bag.Select(TileOf).ToArray(),
            Racks.ToDictionary(pair => SeatOf(pair.Key), pair => (IReadOnlyList<Tile>)pair.Value.Select(TileOf).ToArray()),
            Scores.ToDictionary(pair => SeatOf(pair.Key), pair => pair.Value),
            Playing.Select(SeatOf).ToArray(),
            SeatOf(ToMove),
            SeatOf(Starter),
            ScorelessTurns,
            LastPlay is { } last ? new LastPlay(SeatOf(last.Seat), last.Words, last.Score) : null,
            Winners is { } winners ? new TilesOutcome(winners.Select(SeatOf).ToArray()) : null);

        private static string Letters(IEnumerable<Tile> tiles) => string.Concat(tiles.Select(Letter));

        private static char Letter(Tile tile) =>
            tile.IsBlank && tile.Letter != Tile.Unassigned ? char.ToLowerInvariant(tile.Letter) : tile.Letter;

        private static Tile TileOf(char letter) =>
            letter == Tile.Unassigned ? Tile.Blank
            : char.IsLower(letter) ? Tile.Blank.As(letter)
            : new Tile(letter);

        private static Seat SeatOf(string seat) => Enum.Parse<Seat>(seat);
    }

    private sealed record StoredLastPlay(string Seat, string[] Words, int Score);
}
