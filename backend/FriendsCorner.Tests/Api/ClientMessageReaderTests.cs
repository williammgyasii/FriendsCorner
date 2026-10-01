using FriendsCorner.Api.Contracts;

namespace FriendsCorner.Tests;

public class ClientMessageReaderTests
{
    private readonly ClientMessageReader _reader = new();

    [Fact]
    public void A_signal_is_relayed_as_is()
    {
        const string json = """{"type":"signal","payload":{"kind":"offer"}}""";

        Assert.Equal(new Relay(json), _reader.Read(json));
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public void Each_message_reads_as_its_command(string json, RoomCommand expected)
    {
        Assert.Equal(new Act(expected), _reader.Read(json));
    }

    public static TheoryData<string, RoomCommand> Commands => new()
    {
        { """{"type":"direction","x":1,"y":-0.5}""", new Steer(1, -0.5) },
        { """{"type":"place","square":4}""", new Place(4) },
        { """{"type":"rematch"}""", new Rematch() },
        { """{"type":"chess-move","from":"e2","to":"e4"}""", new MoveChess(new ChessMove("e2", "e4")) },
        { """{"type":"chess-move","from":"a7","to":"a8","promotion":"n"}""", new MoveChess(new ChessMove("a7", "a8", 'n')) },
        { """{"type":"chess-rematch"}""", new Rematch() },
        { """{"type":"pick","game":"chess"}""", new PickGame("chess") },
        { """{"type":"ready","ready":true}""", new SetReady(true) },
        { """{"type":"capacity","size":3}""", new SetCapacity(3) },
        { """{"type":"media","camera":false,"mic":true}""", new ShareMedia(Camera: false, Mic: true) },
        { """{"type":"start"}""", new StartGame() },
    };

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"x":1}""")]
    [InlineData("""{"type":"launch","world":"chess"}""")]
    [InlineData("""{"type":"direction","x":"left","y":0}""")]
    [InlineData("""{"type":"place"}""")]
    [InlineData("""{"type":"chess-move","from":"e2"}""")]
    [InlineData("""{"type":"pick","game":3}""")]
    [InlineData("""{"type":"ready","ready":"yes"}""")]
    [InlineData("""{"type":"capacity","size":"big"}""")]
    [InlineData("""{"type":"media","camera":false}""")]
    public void Anything_else_is_ignored(string json)
    {
        Assert.Null(_reader.Read(json));
    }

    [Fact]
    public void A_tiles_play_reads_each_square_and_letter()
    {
        var read = _reader.Read("""
            {"type":"tiles-play","tiles":[{"square":111,"letter":"c"},{"square":112,"letter":"A","blank":true}]}
            """);

        var play = Assert.IsType<PlayTiles>(Assert.IsType<Act>(read).Command);
        Assert.Equal([new PlacedTile(111, new Tile('C')), new PlacedTile(112, Tile.Blank.As('A'))], play.Tiles);
    }

    [Fact]
    public void A_tiles_preview_reads_like_a_play()
    {
        var read = _reader.Read("""
            {"type":"tiles-preview","tiles":[{"square":111,"letter":"C"},{"square":112,"letter":"a","blank":true}]}
            """);

        var preview = Assert.IsType<PreviewTiles>(Assert.IsType<Act>(read).Command);
        Assert.Equal([new PlacedTile(111, new Tile('C')), new PlacedTile(112, Tile.Blank.As('A'))], preview.Tiles);
    }

    [Fact]
    public void A_tiles_exchange_reads_a_question_mark_as_a_blank()
    {
        var read = _reader.Read("""{"type":"tiles-exchange","letters":"Qe?"}""");

        var exchange = Assert.IsType<ExchangeTiles>(Assert.IsType<Act>(read).Command);
        Assert.Equal([new Tile('Q'), new Tile('E'), Tile.Blank], exchange.Tiles);
    }

    [Theory]
    [InlineData("""{"type":"tiles-pass"}""", typeof(PassTurn))]
    [InlineData("""{"type":"tiles-rematch"}""", typeof(Rematch))]
    public void Tiles_pass_and_rematch_read_as_their_commands(string json, Type expected)
    {
        Assert.IsType(expected, Assert.IsType<Act>(_reader.Read(json)).Command);
    }

    [Theory]
    [InlineData("""{"type":"tiles-play","tiles":[{"square":"x","letter":"C"}]}""")]
    [InlineData("""{"type":"tiles-play","tiles":[{"square":112,"letter":"CA"}]}""")]
    [InlineData("""{"type":"tiles-play","tiles":[{"square":112,"letter":"1"}]}""")]
    [InlineData("""{"type":"tiles-play","tiles":[{"square":112,"letter":"?"}]}""")]
    [InlineData("""{"type":"tiles-play","tiles":[]}""")]
    [InlineData("""{"type":"tiles-play","tiles":"CAT"}""")]
    [InlineData("""{"type":"tiles-preview","tiles":[{"square":"x","letter":"C"}]}""")]
    [InlineData("""{"type":"tiles-preview","tiles":[]}""")]
    [InlineData("""{"type":"tiles-exchange","letters":""}""")]
    [InlineData("""{"type":"tiles-exchange","letters":"C1"}""")]
    public void A_malformed_tiles_message_is_ignored(string json)
    {
        Assert.Null(_reader.Read(json));
    }

    [Fact]
    public void Mystery_open_accuse_and_rematch_read_as_their_commands()
    {
        Assert.Equal(new Act(new OpenLead("c1")), _reader.Read("""{"type":"mystery-open","lead":"c1"}"""));
        Assert.Equal(new Act(new Accuse("s3")), _reader.Read("""{"type":"mystery-accuse","suspect":"s3"}"""));
        Assert.Equal(new Act(new Rematch()), _reader.Read("""{"type":"mystery-rematch"}"""));
        Assert.Equal(new Act(new Withdraw()), _reader.Read("""{"type":"mystery-withdraw"}"""));
    }

    [Fact]
    public void Mystery_settings_read_as_the_settings_command()
    {
        Assert.Equal(
            new Act(new SetMysterySettings(new MysterySettings(MysteryLevel.Hard, MysteryMode.Race))),
            _reader.Read("""{"type":"mystery-settings","level":"hard","mode":"race"}"""));
        Assert.Equal(
            new Act(new SetMysterySettings(MysterySettings.Default)),
            _reader.Read("""{"type":"mystery-settings","level":"easy","mode":"together"}"""));
    }

    [Theory]
    [InlineData("""{"type":"mystery-settings","level":"medium","mode":"race"}""")]
    [InlineData("""{"type":"mystery-settings","level":"hard","mode":"solo"}""")]
    [InlineData("""{"type":"mystery-settings","level":"Hard","mode":"race"}""")]
    [InlineData("""{"type":"mystery-settings","level":"1","mode":"race"}""")]
    [InlineData("""{"type":"mystery-settings","mode":"race"}""")]
    [InlineData("""{"type":"mystery-settings","level":"hard"}""")]
    public void Malformed_mystery_settings_are_ignored(string json)
    {
        Assert.Null(_reader.Read(json));
    }

    [Theory]
    [InlineData("""{"type":"mystery-open"}""")]
    [InlineData("""{"type":"mystery-open","lead":7}""")]
    [InlineData("""{"type":"mystery-open","lead":""}""")]
    [InlineData("""{"type":"mystery-accuse"}""")]
    [InlineData("""{"type":"mystery-accuse","suspect":null}""")]
    public void A_malformed_mystery_message_is_ignored(string json)
    {
        Assert.Null(_reader.Read(json));
    }

    [Fact]
    public void A_promotion_longer_than_one_letter_is_dropped()
    {
        var read = _reader.Read("""{"type":"chess-move","from":"a7","to":"a8","promotion":"queen"}""");

        Assert.Equal(new Act(new MoveChess(new ChessMove("a7", "a8"))), read);
    }
}
