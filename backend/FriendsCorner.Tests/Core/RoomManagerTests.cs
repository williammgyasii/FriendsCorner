using System.Net.WebSockets;
using System.Text;
using FriendsCorner.Core.Accessors;
using FriendsCorner.Infrastructure.Accessors;
using FriendsCorner.Core.Managers;
using FriendsCorner.Core.Utilities;
using FriendsCorner.Infrastructure.Utilities;

namespace FriendsCorner.Tests;

public class RoomManagerTests
{
    private static RoomManager NewRoom(Action? onEmpty = null, IGameRecorderAccessor? games = null) =>
        new(
            "room-1",
            onEmpty ?? (() => { }),
            new RoomEngine(),
            new SeatSocketAccessor(new WebSocketTextUtility()),
            new TickerUtility(),
            new StateMessageAccessor(),
            games ?? new FakeGameRecorder(),
            TimeProvider.System);

    [Fact]
    public async Task An_accepted_game_move_notes_the_running_game_once()
    {
        var games = new FakeGameRecorder();
        var room = NewRoom(games: games);
        room.Restore(new TicTacToeGame(Board.Empty()));
        await room.Join(new FakeSocket(), CancellationToken.None);

        await room.Act(Seat.A, new Place(4), CancellationToken.None);
        await room.Act(Seat.A, new Place(0), CancellationToken.None);

        var noted = Assert.Single(games.Noted);
        Assert.Equal("room-1", noted.RoomId);
        Assert.Equal('X', noted.Board[4]);
    }

    [Fact]
    public async Task A_restored_game_is_what_the_room_shows()
    {
        var room = NewRoom();
        room.Restore(new ChessGame(ChessBoard.Start()));
        var socket = new FakeSocket();

        await room.Join(socket, CancellationToken.None);

        Assert.Contains("\"world\":\"chess\"", socket.Sent[1]);
    }

    [Fact]
    public async Task Joining_names_the_seat_then_sends_the_state()
    {
        var room = NewRoom();
        var socket = new FakeSocket();

        var seat = await room.Join(socket, CancellationToken.None);

        Assert.Equal(Seat.A, seat);
        Assert.Equal("""{"type":"joined","seat":"A"}""", socket.Sent[0]);
        Assert.StartsWith("""{"type":"state","you":"A",""", socket.Sent[1]);
    }

    [Fact]
    public async Task A_full_room_turns_the_newcomer_away()
    {
        var room = NewRoom();
        await room.Join(new FakeSocket(), CancellationToken.None);
        await room.Join(new FakeSocket(), CancellationToken.None);

        Assert.Null(await room.Join(new FakeSocket(), CancellationToken.None));
    }

    [Fact]
    public async Task A_relay_reaches_only_the_call_partner()
    {
        var room = NewRoom();
        var a = new FakeSocket();
        var b = new FakeSocket();
        await room.Join(a, CancellationToken.None);
        await room.Join(b, CancellationToken.None);
        const string signal = """{"type":"signal","payload":{"kind":"offer"}}""";

        await room.Relay(Seat.A, signal, CancellationToken.None);

        Assert.Contains(signal, b.Sent);
        Assert.DoesNotContain(signal, a.Sent);
    }

    [Fact]
    public async Task A_command_that_changes_the_room_is_shown_to_everyone()
    {
        var room = NewRoom();
        var a = new FakeSocket();
        var b = new FakeSocket();
        await room.Join(a, CancellationToken.None);
        await room.Join(b, CancellationToken.None);

        await room.Act(Seat.A, new PickGame("chess"), CancellationToken.None);

        Assert.Contains(a.Sent, json => json.Contains("\"pick\":\"chess\""));
        Assert.Contains(b.Sent, json => json.Contains("\"pick\":\"chess\""));
    }

    [Fact]
    public async Task A_chess_move_is_noted_for_saving()
    {
        var chess = new ChessRecorderAccessor();
        var room = NewRoom(games: new GameRecorderAccessor(new BoardRecorderAccessor(), chess, new LetterTilesRecorderAccessor()));
        room.Restore(new ChessGame(ChessBoard.Start()));
        await room.Join(new FakeSocket(), CancellationToken.None);

        await room.Act(Seat.A, new MoveChess(new ChessMove("e2", "e4")), CancellationToken.None);

        var noted = Assert.Single(chess.TakeWaiting());
        Assert.Equal("room-1", noted.RoomId);
        Assert.Equal(Seat.B, noted.Board.ToMove);
    }

    [Fact]
    public async Task The_last_one_out_closes_the_room()
    {
        var closed = false;
        var room = NewRoom(() => closed = true);
        await room.Join(new FakeSocket(), CancellationToken.None);
        await room.Join(new FakeSocket(), CancellationToken.None);

        await room.Leave(Seat.A);
        Assert.False(closed);

        await room.Leave(Seat.B);
        Assert.True(closed);
    }

    private static readonly PlacedTile[] Cat =
        [new(111, new Tile('C')), new(112, new Tile('A')), new(113, new Tile('T'))];

    private static LetterTilesGame TilesWithCat()
    {
        var words = new WordsAndScoreTests.FakeWords("CAT");
        var start = LetterTilesState.Start([Seat.A, Seat.B], Seat.A, new Random(7));
        var racks = new Dictionary<Seat, IReadOnlyList<Tile>>
        {
            [Seat.A] = "CATSEEN".Select(letter => new Tile(letter)).ToArray(),
            [Seat.B] = "BDFGHIO".Select(letter => new Tile(letter)).ToArray(),
        };
        return new LetterTilesGame(start with { Racks = racks }, words, new Random(7));
    }

    [Fact]
    public async Task A_preview_is_answered_to_the_asking_seat_only_and_never_saved()
    {
        var games = new CountingRecorder();
        var room = NewRoom(games: games);
        room.Restore(TilesWithCat());
        var a = new FakeSocket();
        var b = new FakeSocket();
        await room.Join(a, CancellationToken.None);
        await room.Join(b, CancellationToken.None);
        var (aBefore, bBefore) = (a.Sent.Count, b.Sent.Count);

        await room.Act(Seat.A, new PreviewTiles(Cat), CancellationToken.None);

        var answer = Assert.Single(a.Sent.Skip(aBefore));
        Assert.StartsWith("""{"type":"tiles-preview",""", answer);
        Assert.Contains("\"score\":10", answer);
        Assert.Equal(bBefore, b.Sent.Count);
        Assert.Equal(0, games.Notes);
    }

    [Fact]
    public async Task A_question_with_no_answer_sends_nothing()
    {
        var room = NewRoom();
        room.Restore(new TicTacToeGame(Board.Empty()));
        var a = new FakeSocket();
        await room.Join(a, CancellationToken.None);
        var before = a.Sent.Count;

        await room.Act(Seat.A, new PreviewTiles(Cat), CancellationToken.None);

        Assert.Equal(before, a.Sent.Count);
    }

    private sealed class CountingRecorder : IGameRecorderAccessor
    {
        public int Notes { get; private set; }

        public void Note(string roomId, IGameEngine game) => Notes++;
    }

    // Copies what the game showed at the moment it was noted.
    private sealed class FakeGameRecorder : IGameRecorderAccessor
    {
        public List<(string RoomId, char?[] Board)> Noted { get; } = [];

        public void Note(string roomId, IGameEngine game)
        {
            if (game is TicTacToeGame ticTacToe)
            {
                Noted.Add((roomId, ticTacToe.Board.Squares.ToArray()));
            }
        }
    }

    private sealed class FakeSocket : WebSocket
    {
        public List<string> Sent { get; } = [];

        public override WebSocketCloseStatus? CloseStatus => null;

        public override string? CloseStatusDescription => null;

        public override WebSocketState State => WebSocketState.Open;

        public override string? SubProtocol => null;

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            lock (Sent)
            {
                Sent.Add(Encoding.UTF8.GetString(buffer));
            }

            return Task.CompletedTask;
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public override void Abort()
        {
        }

        public override void Dispose()
        {
        }
    }
}
