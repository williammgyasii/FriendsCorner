using System.Net.WebSockets;
using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;
using FriendsCorner.Core.Engines.Games;
using FriendsCorner.Core.Engines.Mystery;
using FriendsCorner.Core.Utilities;

namespace FriendsCorner.Core.Managers;

public interface IRoomManager
{
    void Restore(IGameEngine game);

    Task<Seat?> Join(WebSocket socket, CancellationToken cancellationToken);

    Task<Seat?> Join(WebSocket socket, Guid userId, string gameName, CancellationToken cancellationToken);

    void RememberHost(Guid userId);

    Task Act(Seat seat, RoomCommand command, CancellationToken cancellationToken);

    Task Relay(Seat from, string json, CancellationToken cancellationToken);

    Task Leave(Seat seat);

    Task Leave(Seat seat, WebSocket socket) => Leave(seat);
}

// One live room's use cases: join, act, relay, tick, leave. The rules are the
// engine's; this only puts each step in order and says who hears about it.
public sealed class RoomManager : IRoomManager
{
    private static readonly TimeSpan TickLength = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan CaseTimeLimit = TimeSpan.FromSeconds(60);
    private const int CaseAttempts = 2;

    private readonly Lock _gate = new();
    private readonly string _roomId;
    private readonly Action _onEmpty;
    private readonly RoomEngine _room;
    private readonly ISeatSocketAccessor _sockets;
    private readonly ITickerUtility _clock;
    private readonly IStateMessageAccessor _state;
    private readonly IGameRecorderAccessor _games;
    private readonly ICaseWriterAccessor _cases;
    private readonly TimeProvider _time;

    public RoomManager(
        string roomId,
        Action onEmpty,
        RoomEngine room,
        ISeatSocketAccessor sockets,
        ITickerUtility clock,
        IStateMessageAccessor state,
        IGameRecorderAccessor games,
        ICaseWriterAccessor cases,
        TimeProvider time)
    {
        _roomId = roomId;
        _onEmpty = onEmpty;
        _room = room;
        _sockets = sockets;
        _clock = clock;
        _state = state;
        _games = games;
        _cases = cases;
        _time = time;
    }

    // The write in flight, if any; tests await it.
    public Task CaseWriting { get; private set; } = Task.CompletedTask;

    private int? _writingFor;

    public void Restore(IGameEngine game)
    {
        lock (_gate)
        {
            _room.Restore(game);
        }
    }

    public void RememberHost(Guid userId)
    {
        lock (_gate)
        {
            _room.Lobby.AssignHost(userId);
        }
    }

    public Task<Seat?> Join(WebSocket socket, CancellationToken cancellationToken) =>
        Sit(socket, null, cancellationToken);

    public Task<Seat?> Join(WebSocket socket, Guid userId, string gameName, CancellationToken cancellationToken) =>
        Sit(socket, (userId, gameName), cancellationToken);

    private async Task<Seat?> Sit(WebSocket socket, (Guid UserId, string GameName)? account, CancellationToken cancellationToken)
    {
        Seat seat;
        WebSocket? previous = null;
        lock (_gate)
        {
            var sat = account is { } known
                ? _room.TrySit(known.UserId, known.GameName, out seat)
                : _room.TryAddSeat(out seat);
            if (!sat)
            {
                return null;
            }

            previous = _sockets.Replace(seat, socket);
        }

        previous?.Abort();
        await _sockets.Send(seat, _state.Joined(seat), cancellationToken);
        await Broadcast(cancellationToken);
        _clock.Start(TickLength, Tick);
        return seat;
    }

    public async Task Act(Seat seat, RoomCommand command, CancellationToken cancellationToken)
    {
        if (command is GameQuestion question)
        {
            await Answer(seat, question, cancellationToken);
            return;
        }

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

        WriteAwaitedCase(change);
    }

    // A question changes nothing, so it is neither recorded nor broadcast.
    private Task Answer(Seat seat, GameQuestion question, CancellationToken cancellationToken)
    {
        string? json;
        lock (_gate)
        {
            json = _room.Ask(seat, question) is { } answer ? _state.Answer(answer) : null;
        }

        return json is null ? Task.CompletedTask : _sockets.Send(seat, json, cancellationToken);
    }

    public Task Relay(Seat from, string json, CancellationToken cancellationToken) =>
        RoomEngine.CallPartner(from) is { } partner
            ? _sockets.Send(partner, json, cancellationToken)
            : Task.CompletedTask;

    public Task Leave(Seat seat) => LeaveSeat(seat);

    public Task Leave(Seat seat, WebSocket socket) =>
        _sockets.Holds(seat, socket) ? LeaveSeat(seat) : Task.CompletedTask;

    private async Task LeaveSeat(Seat seat)
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
        RoomChange change;
        lock (_gate)
        {
            change = _room.Advance(TickLength.TotalSeconds, _time.GetUtcNow());
            Record(change);
        }

        WriteAwaitedCase(change);
        return Broadcast(CancellationToken.None);
    }

    // A game that needs a case gets one write at a time, run off the gate so
    // the room keeps ticking. The request number rides along to the engine.
    private void WriteAwaitedCase(RoomChange change)
    {
        if (!change.HasFlag(RoomChange.Game))
        {
            return;
        }

        int request;
        MysteryLevel level;
        lock (_gate)
        {
            if (_room.Game is not IAwaitsCase { WaitingFor: { } waiting } game || waiting == _writingFor)
            {
                return;
            }

            _writingFor = request = waiting;
            level = game.Level;
        }

        CaseWriting = WriteCase(request, new CaseRequest(CaseThemes.Pick(Random.Shared), level));
    }

    private async Task WriteCase(int request, CaseRequest asked)
    {
        for (var attempt = 1; attempt <= CaseAttempts; attempt++)
        {
            Case? written;
            using var limit = new CancellationTokenSource(CaseTimeLimit, _time);
            try
            {
                written = await _cases.Write(asked, limit.Token).WaitAsync(limit.Token);
            }
            catch (OperationCanceledException) when (limit.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                written = null;
            }

            if (written is not null && CaseCheck.Check(written) == CaseVerdict.Fair)
            {
                await HandOver(game => game.TryReceive(request, written));
                return;
            }
        }

        await HandOver(game => game.TryFail(request));
    }

    private Task HandOver(Func<IAwaitsCase, bool> change)
    {
        lock (_gate)
        {
            if (_room.Game is not IAwaitsCase game || !change(game))
            {
                return Task.CompletedTask;
            }

            Record(RoomChange.Game);
        }

        return Broadcast(CancellationToken.None);
    }

    // Call inside the gate so the board noted is the one the change made.
    private void Record(RoomChange change)
    {
        if (change.HasFlag(RoomChange.Game) && _room.Game is { } game)
        {
            _games.Note(_roomId, game);
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
