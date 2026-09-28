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
        { """{"type":"chess-rematch"}""", new ChessRematch() },
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
    public void A_promotion_longer_than_one_letter_is_dropped()
    {
        var read = _reader.Read("""{"type":"chess-move","from":"a7","to":"a8","promotion":"queen"}""");

        Assert.Equal(new Act(new MoveChess(new ChessMove("a7", "a8"))), read);
    }
}
