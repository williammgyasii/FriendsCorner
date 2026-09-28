using System.Net.WebSockets;
using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;
using FriendsCorner.Core.Utilities;

namespace FriendsCorner.Core.Managers;

public interface IRoomManager
{
    void RestoreTicTacToe(Board board);

    void RestoreChess(ChessBoard board);

    Task<Seat?> Join(WebSocket socket, CancellationToken cancellationToken);

    Task Act(Seat seat, RoomCommand command, CancellationToken cancellationToken);

    Task Relay(Seat from, string json, CancellationToken cancellationToken);

    Task Leave(Seat seat);
}

// One live room's use cases: join, act, relay, tick, leave. The rules are the
// engine's; this only puts each step in order and says who hears about it.
public sealed class RoomManager : IRoomManager
{
    private static readonly TimeSpan TickLength = TimeSpan.FromMilliseconds(50);

    private readonly Lock _gate = new();
    private readonly string _roomId;
    private readonly Action _onEmpty;
    private readonly RoomEngine _room;
    private readonly ISeatSocketAccessor _sockets;
    private readonly ITickerUtility _clock;
    private readonly IStateMessageAccessor _state;
    private readonly IBoardRecorderAccessor _boards;
    private readonly IChessRecorderAccessor _chess;
    private readonly TimeProvider _time;

    public RoomManager(
        string roomId,
        Action onEmpty,
        RoomEngine room,
        ISeatSocketAccessor sockets,
        ITickerUtility clock,
        IStateMessageAccessor state,
        IBoardRecorderAccessor boards,
        IChessRecorderAccessor chess,
        TimeProvider time)
    {
        _roomId = roomId;
        _onEmpty = onEmpty;
        _room = room;
        _sockets = sockets;
        _clock = clock;
        _state = state;
        _boards = boards;
        _chess = chess;
        _time = time;
    }

    public void RestoreTicTacToe(Board board) => _room.RestoreTicTacToe(board);

    public void RestoreChess(ChessBoard board) => _room.RestoreChess(board);

    public async Task<Seat?> Join(WebSocket socket, CancellationToken cancellationToken)
    {
        Seat seat;
        lock (_gate)
        {
            if (!_room.TryAddSeat(out seat))
            {
                return null;
            }

            _sockets.Add(seat, socket);
        }

        await _sockets.Send(seat, _state.Joined(seat), cancellationToken);
        await Broadcast(cancellationToken);
        _clock.Start(TickLength, Tick);
        return seat;
    }

    public async Task Act(Seat seat, RoomCommand command, CancellationToken cancellationToken)
    {
        RoomChange change;
        lock (_gate)
        {
            change = _room.Apply(seat, command, _time.GetUtcNow());
            Record(change);
        }

        if (change != RoomChange.None)
        {
            await Broadcast(cancellationToken);
        }
    }

    public Task Relay(Seat from, string json, CancellationToken cancellationToken) =>
        RoomEngine.CallPartner(from) is { } partner
            ? _sockets.Send(partner, json, cancellationToken)
            : Task.CompletedTask;

    public async Task Leave(Seat seat)
    {
        bool empty;
        lock (_gate)
        {
            _room.Free(seat);
            empty = _sockets.Remove(seat);
        }

        if (empty)
        {
            _clock.Stop();
            _onEmpty();
            return;
        }

        await Broadcast(CancellationToken.None);
    }

    private Task Tick()
    {
        lock (_gate)
        {
            Record(_room.Advance(TickLength.TotalSeconds, _time.GetUtcNow()));
        }

        return Broadcast(CancellationToken.None);
    }

    // Call inside the gate so the board noted is the one the change made.
    private void Record(RoomChange change)
    {
        if (change.HasFlag(RoomChange.TicTacToe) && _room.TicTacToe is { } board)
        {
            _boards.Note(_roomId, board);
        }

        if (change.HasFlag(RoomChange.Chess) && _room.Chess is { } chess)
        {
            _chess.Note(_roomId, chess);
        }
    }

    private Task Broadcast(CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        List<(Seat, string)> messages;
        lock (_gate)
        {
            messages = _sockets.Seats.Select(seat => (seat, _state.Write(_room, seat, now))).ToList();
        }

        return _sockets.SendEach(messages, cancellationToken);
    }
}
