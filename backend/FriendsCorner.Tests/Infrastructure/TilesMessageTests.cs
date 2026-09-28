using System.Text.Json;
using FriendsCorner.Infrastructure.Accessors;

namespace FriendsCorner.Tests;

// What each seat is told about a Letter Tiles game. The racks are the secret:
// a seat must never be sent another seat's letters or the order of the bag.
public class TilesMessageTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 20, 0, 0, TimeSpan.Zero);
    private static readonly WordsAndScoreTests.FakeWords Words = new("AT");
    private readonly StateMessageAccessor _state = new();

    private static Tile[] Rack(string letters) =>
        letters.Select(letter => letter == Tile.Unassigned ? Tile.Blank : new Tile(letter)).ToArray();

    // A holds AEIOUR?, B holds FGHJKMP: none of B's letters is also a seat name.
    private static (RoomEngine Room, LetterTilesGame Game) TwoPlayerGame()
    {
        var room = new RoomEngine();
        Assert.True(room.TryAddSeat(out _));
        Assert.True(room.TryAddSeat(out _));
        var start = LetterTilesState.Start([Seat.A, Seat.B], Seat.A, new Random(7));
        var game = new LetterTilesGame(
            start with
            {
                Bag = Rack("SSSTTTT"),
                Racks = new Dictionary<Seat, IReadOnlyList<Tile>> { [Seat.A] = Rack("AEIOUR?"), [Seat.B] = Rack("FGHJKMP") },
            },
            Words,
            new Random(7));
        room.Restore(game);
        return (room, game);
    }

    private JsonElement Tiles(RoomEngine room, Seat you) =>
        JsonDocument.Parse(_state.Write(room, you, Now)).RootElement.GetProperty("tiles").Clone();

    private static IEnumerable<string> StringsIn(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => [element.GetString()!],
        JsonValueKind.Array => element.EnumerateArray().SelectMany(StringsIn),
        JsonValueKind.Object => element.EnumerateObject().Where(p => p.Name != "values").SelectMany(p => StringsIn(p.Value)),
        _ => [],
    };

    [Fact]
    public void Each_seat_sees_its_own_rack_and_only_a_count_for_the_other()
    {
        var (room, _) = TwoPlayerGame();

        var a = Tiles(room, Seat.A);
        var b = Tiles(room, Seat.B);

        Assert.Equal(["A", "E", "I", "O", "U", "R", "?"], a.GetProperty("rack").EnumerateArray().Select(t => t.GetString()));
        Assert.Equal(["F", "G", "H", "J", "K", "M", "P"], b.GetProperty("rack").EnumerateArray().Select(t => t.GetString()));
        Assert.Equal(
            """[{"seat":"A","score":0,"count":7},{"seat":"B","score":0,"count":7}]""",
            a.GetProperty("players").GetRawText());
        Assert.Equal(7, a.GetProperty("bag").GetInt32());
    }

    [Fact]
    public void No_message_carries_another_seats_letters_or_the_bag()
    {
        var (room, _) = TwoPlayerGame();

        var everythingA = StringsIn(JsonDocument.Parse(_state.Write(room, Seat.A, Now)).RootElement).ToHashSet();

        Assert.DoesNotContain(everythingA, text => "FGHJKMPST".Contains(text) && text.Length == 1);
    }

    [Fact]
    public void A_seat_that_is_not_playing_sees_an_empty_rack()
    {
        var (room, _) = TwoPlayerGame();

        Assert.Equal(0, Tiles(room, Seat.C).GetProperty("rack").GetArrayLength());
    }

    [Fact]
    public void The_board_layout_values_and_turn_are_shared()
    {
        var (room, _) = TwoPlayerGame();

        var tiles = Tiles(room, Seat.A);

        Assert.Equal(PremiumLayout.Text, tiles.GetProperty("layout").GetString());
        Assert.Equal(225, tiles.GetProperty("board").GetArrayLength());
        Assert.Equal(10, tiles.GetProperty("values").GetProperty("Q").GetInt32());
        Assert.Equal(0, tiles.GetProperty("values").GetProperty("?").GetInt32());
        Assert.Equal("A", tiles.GetProperty("toMove").GetString());
        Assert.Equal(JsonValueKind.Null, tiles.GetProperty("lastPlay").ValueKind);
        Assert.Equal(JsonValueKind.Null, tiles.GetProperty("outcome").ValueKind);
        Assert.Equal(JsonValueKind.Null, tiles.GetProperty("refusal").ValueKind);
    }

    [Fact]
    public void A_played_blank_shows_in_lower_case_and_the_last_play_is_named()
    {
        var (room, game) = TwoPlayerGame();
        Assert.True(game.TryPlay(Seat.A, new PlayTiles([new PlacedTile(112, new Tile('A')), new PlacedTile(113, Tile.Blank.As('T'))])));

        var tiles = Tiles(room, Seat.B);

        Assert.Equal(["A", "t"], tiles.GetProperty("board").EnumerateArray().Skip(112).Take(2).Select(t => t.GetString()));
        Assert.Equal(JsonValueKind.Null, tiles.GetProperty("board")[0].ValueKind);
        Assert.Equal("""{"seat":"A","words":["AT"],"score":2}""", tiles.GetProperty("lastPlay").GetRawText());
    }

    [Fact]
    public void A_refusal_is_told_only_to_the_seat_that_was_refused()
    {
        var (room, game) = TwoPlayerGame();
        Assert.False(game.TryPlay(Seat.A, new PlayTiles([new PlacedTile(111, new Tile('E')), new PlacedTile(112, new Tile('A'))])));

        Assert.Equal(
            """{"reason":"not-a-word","words":["EA"]}""",
            Tiles(room, Seat.A).GetProperty("refusal").GetRawText());
        Assert.Equal(JsonValueKind.Null, Tiles(room, Seat.B).GetProperty("refusal").ValueKind);
    }

    [Fact]
    public void The_outcome_names_the_winners_and_final_scores()
    {
        var (room, game) = TwoPlayerGame();
        for (var turn = 0; turn < 6; turn++)
        {
            Assert.True(game.TryPlay(game.State.ToMove, new PassTurn()));
        }

        var outcome = Tiles(room, Seat.A).GetProperty("outcome");

        Assert.Equal("""["A"]""", outcome.GetProperty("winners").GetRawText());
        Assert.Equal("""{"A":-6,"B":-29}""", outcome.GetProperty("scores").GetRawText());
    }

    private static readonly PlacedTile[] Asked = [new(111, new Tile('C')), new(112, Tile.Blank.As('A'))];

    [Fact]
    public void A_preview_answer_echoes_the_tiles_with_the_words_and_score()
    {
        Assert.Equal(
            """{"type":"tiles-preview","tiles":[{"square":111,"letter":"C","blank":false},{"square":112,"letter":"A","blank":true}],"words":["CA"],"score":3}""",
            _state.Answer(new TilesPreview(Asked, ["CA"], 3)));
    }

    [Fact]
    public void A_refused_preview_answer_carries_the_reason_code()
    {
        Assert.Equal(
            """{"type":"tiles-preview","tiles":[{"square":111,"letter":"C","blank":false},{"square":112,"letter":"A","blank":true}],"refusal":{"reason":"not-a-word","words":["CA"]}}""",
            _state.Answer(new TilesPreviewRefused(Asked, new Refusal(RefusalReason.NotAWord, ["CA"]))));
    }
}
