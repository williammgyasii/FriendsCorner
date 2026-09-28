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
    private static RoomManager NewRoom(Action? onEmpty = null, ChessRecorderAccessor? chess = null) =>
        new(
            "room-1",
            onEmpty ?? (() => { }),
            new RoomEngine(),
            new SeatSocketAccessor(new WebSocketTextUtility()),
            new TickerUtility(),
            new StateMessageAccessor(),
            new BoardRecorderAccessor(),
            chess ?? new ChessRecorderAccessor(),
            TimeProvider.System);

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
        var room = NewRoom(chess: chess);
        room.RestoreChess(ChessBoard.Start());
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
