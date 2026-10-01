using System.Text.Json;
using FriendsCorner.Core.Accessors;
using FriendsCorner.Infrastructure.Accessors;

namespace FriendsCorner.Tests;

// Pins the exact JSON the browser reads, so the page keeps working as the
// server around it changes.
public class StateMessageAccessorTests
{
    private readonly StateMessageAccessor _state = new();

    private static readonly DateTimeOffset Now = new(2026, 9, 27, 20, 0, 0, TimeSpan.Zero);

    private static RoomEngine TwoInTheLobby()
    {
        var room = new RoomEngine();
        Assert.True(room.TryAddSeat(out _));
        Assert.True(room.TryAddSeat(out _));
        return room;
    }

    [Fact]
    public void Joined_names_the_seat()
    {
        Assert.Equal("""{"type":"joined","seat":"C"}""", _state.Joined(Seat.C));
    }

    [Fact]
    public void A_lobby_state_matches_the_wire_format()
    {
        var room = TwoInTheLobby();
        room.Apply(Seat.A, new PickGame("chess"), Now);

        var json = _state.Write(room, Seat.B, Now);

        const string expected = """
            {"type":"state","you":"B","world":null,"board":null,"chess":null,"tiles":null,"mystery":null,
            "players":{"A":{"x":240,"y":160},"B":{"x":240,"y":160},"C":null,"D":null},
            "lobby":{"host":"A","capacity":2,"pick":"chess","canStart":false,"countdownMs":null,
            "members":[{"seat":"A","gameName":"Player","ready":false,"camera":true,"mic":true,"playing":true},
            {"seat":"B","gameName":"Player","ready":false,"camera":true,"mic":true,"playing":true}],
            "mystery":{"level":"easy","mode":"together"}}}
            """;
        Assert.Equal(expected.Replace("\n", ""), json);
    }

    [Fact]
    public void The_countdown_is_the_milliseconds_left()
    {
        var room = TwoInTheLobby();
        room.Apply(Seat.A, new PickGame("chess"), Now);
        room.Apply(Seat.B, new SetReady(true), Now);
        room.Apply(Seat.A, new StartGame(), Now);

        using var state = JsonDocument.Parse(_state.Write(room, Seat.A, Now.AddMilliseconds(1200)));
        var lobby = state.RootElement.GetProperty("lobby");

        Assert.True(lobby.GetProperty("canStart").GetBoolean());
        Assert.Equal(1800, lobby.GetProperty("countdownMs").GetInt32());
    }

    [Fact]
    public void A_tic_tac_toe_board_matches_the_wire_format()
    {
        var room = TwoInTheLobby();
        Assert.True(room.TryLaunch("tictactoe"));
        room.Apply(Seat.A, new Place(0), Now);

        using var state = JsonDocument.Parse(_state.Write(room, Seat.A, Now));

        Assert.Equal("tictactoe", state.RootElement.GetProperty("world").GetString());
        Assert.Equal(JsonValueKind.Null, state.RootElement.GetProperty("tiles").ValueKind);
        Assert.Equal(JsonValueKind.Null, state.RootElement.GetProperty("mystery").ValueKind);
        Assert.Equal(
            """{"squares":["X",null,null,null,null,null,null,null,null],"next":"B","winner":null,"draw":false}""",
            state.RootElement.GetProperty("board").GetRawText());
    }

    [Fact]
    public void A_chess_board_matches_the_wire_format()
    {
        var room = TwoInTheLobby();
        Assert.True(room.TryLaunch("chess"));
        room.Apply(Seat.A, new MoveChess(new ChessMove("e2", "e4")), Now);

        using var state = JsonDocument.Parse(_state.Write(room, Seat.A, Now));
        var chess = state.RootElement.GetProperty("chess");

        Assert.Equal(room.RunningChess().Fen, chess.GetProperty("fen").GetString());
        Assert.Equal(JsonValueKind.Null, state.RootElement.GetProperty("tiles").ValueKind);
        Assert.Equal(JsonValueKind.Null, state.RootElement.GetProperty("mystery").ValueKind);
        Assert.Equal("A", chess.GetProperty("white").GetString());
        Assert.Equal("B", chess.GetProperty("toMove").GetString());
        Assert.False(chess.GetProperty("inCheck").GetBoolean());
        Assert.Equal("""{"from":"e2","to":"e4"}""", chess.GetProperty("lastMove").GetRawText());
        Assert.Equal(JsonValueKind.Null, chess.GetProperty("outcome").ValueKind);
        Assert.Equal(20, chess.GetProperty("legalMoves").GetArrayLength());
        Assert.Contains(
            """{"from":"a7","to":"a6","promotion":null}""",
            chess.GetProperty("legalMoves").EnumerateArray().Select(move => move.GetRawText()));
    }
}
