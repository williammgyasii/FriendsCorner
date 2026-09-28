using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FriendsCorner;
using FriendsCorner.Storage;

namespace FriendsCorner.Server;

public sealed class RoomRegistry
{
    private readonly Dictionary<string, RoomSession> _rooms = new();
    private readonly Lock _gate = new();
    private readonly BoardTable _boards;
    private readonly ChessTable _chess;
    private readonly BoardRecorder _recorder;
    private readonly ChessRecorder _chessRecorder;

    public RoomRegistry(BoardTable boards, ChessTable chess, BoardRecorder recorder, ChessRecorder chessRecorder)
    {
        _boards = boards;
        _chess = chess;
        _recorder = recorder;
        _chessRecorder = chessRecorder;
    }

    public string Create()
    {
        var id = Guid.NewGuid().ToString("N");
        lock (_gate)
        {
            _rooms[id] = NewSession(id);
        }

        return id;
    }

    public bool TryGet(string id, out RoomSession? session)
    {
        lock (_gate)
        {
            return _rooms.TryGetValue(id, out session);
        }
    }

    public async Task<RoomSession?> Find(string id)
    {
        lock (_gate)
        {
            if (_rooms.TryGetValue(id, out var existing))
            {
                return existing;
            }
        }

        var board = await _boards.Load(id);
        var chess = board is null ? await _chess.Load(id) : null;
        if (board is null && chess is null)
        {
            return null;
        }

        lock (_gate)
        {
            if (_rooms.TryGetValue(id, out var existing))
            {
                return existing;
            }

            var session = NewSession(id);
            if (board is not null)
            {
                session.RestoreTicTacToe(board);
            }
            else
            {
                session.RestoreChess(chess!);
            }

            _rooms[id] = session;
            return session;
        }
    }

    private RoomSession NewSession(string id) => new(id, () => Remove(id), _recorder, _chessRecorder);

    private void Remove(string id)
    {
        lock (_gate)
        {
            _rooms.Remove(id);
        }
    }
}

public sealed class RoomSession
{
    private readonly Room _room = new();
    private readonly string _roomId;
    private readonly BoardRecorder _recorder;
    private readonly ChessRecorder _chessRecorder;
    private readonly Action _onEmpty;
    private readonly Lock _gate = new();
    private readonly Dictionary<Seat, WebSocket> _sockets = new();
    private readonly CancellationTokenSource _stopping = new();
    private int _clockStarted;

    public RoomSession(string roomId, Action onEmpty, BoardRecorder recorder, ChessRecorder chessRecorder)
    {
        _roomId = roomId;
        _onEmpty = onEmpty;
        _recorder = recorder;
        _chessRecorder = chessRecorder;
    }

    public void RestoreTicTacToe(Board board) => _room.RestoreTicTacToe(board);

    public void RestoreChess(ChessBoard board) => _room.RestoreChess(board);

    public async Task Join(WebSocket socket, CancellationToken cancellationToken)
    {
        if (!TryJoinSeat(socket, out var seat))
        {
            await socket.CloseAsync(
                WebSocketCloseStatus.PolicyViolation,
                "room is full",
                CancellationToken.None);
            return;
        }

        StartClock();
        await Send(socket, JsonSerializer.Serialize(new { type = "joined", seat = seat.ToString() }), cancellationToken);
        await Broadcast(cancellationToken, tick: false);

        try
        {
            await Read(socket, cancellationToken);
        }
        finally
        {
            var empty = Release(seat);
            if (empty)
            {
                _stopping.Cancel();
                _onEmpty();
            }
            else
            {
                await Broadcast(CancellationToken.None, tick: false);
            }
        }
    }

    private bool TryJoinSeat(WebSocket socket, out Seat seat)
    {
        lock (_gate)
        {
            if (!_room.TryAddSeat(out seat))
            {
                return false;
            }

            _sockets[seat] = socket;
            return true;
        }
    }

    private Seat SeatFor(WebSocket socket)
    {
        lock (_gate)
        {
            return _sockets.Single(pair => pair.Value == socket).Key;
        }
    }

    private bool Release(Seat seat)
    {
        lock (_gate)
        {
            _room.Free(seat);
            _sockets.Remove(seat);
            return _sockets.Count == 0;
        }
    }

    private async Task Read(WebSocket socket, CancellationToken cancellationToken)
    {
        var seat = SeatFor(socket);
        var buffer = new byte[4096];
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var message = await ReceiveText(socket, buffer, cancellationToken);
            if (message is null)
            {
                break;
            }

            Applied applied;
            lock (_gate)
            {
                applied = RoomMessage.Apply(_room, seat, message);
            }

            if (applied.Forward is not null)
            {
                await Forward(seat, applied.Forward, cancellationToken);
            }

            if (applied.OpenedWorld || applied.ChangedBoard || applied.ChangedChess)
            {
                await Broadcast(cancellationToken, tick: false);
            }

            if (applied.ChangedBoard && _room.TicTacToe is not null)
            {
                _recorder.Note(_roomId, _room.TicTacToe);
            }

            if (applied.ChangedChess && _room.Chess is not null)
            {
                _chessRecorder.Note(_roomId, _room.Chess);
            }
        }
    }

    private async Task Forward(Seat from, string json, CancellationToken cancellationToken)
    {
        WebSocket? other;
        lock (_gate)
        {
            var otherSeat = from == Seat.A ? Seat.B : Seat.A;
            _sockets.TryGetValue(otherSeat, out other);
        }

        if (other is { State: WebSocketState.Open })
        {
            await Send(other, json, cancellationToken);
        }
    }

    private static async Task<string?> ReceiveText(WebSocket socket, byte[] buffer, CancellationToken cancellationToken)
    {
        using var message = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            message.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(message.ToArray());
    }

    private void StartClock()
    {
        if (Interlocked.Exchange(ref _clockStarted, 1) != 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
            try
            {
                while (await timer.WaitForNextTickAsync(_stopping.Token))
                {
                    await Broadcast(CancellationToken.None, tick: true);
                }
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    private async Task Broadcast(CancellationToken cancellationToken, bool tick)
    {
        List<(WebSocket Socket, string Json)> messages;
        lock (_gate)
        {
            if (tick)
            {
                _room.Tick(0.05);
            }

            messages = _sockets
                .Select(pair => (pair.Value, StateJson(pair.Key)))
                .ToList();
        }

        foreach (var (socket, json) in messages)
        {
            if (socket.State == WebSocketState.Open)
            {
                await Send(socket, json, cancellationToken);
            }
        }
    }

    private string StateJson(Seat you)
    {
        object? Player(Seat seat) =>
            _room.Positions.TryGetValue(seat, out var position)
                ? new { x = position.X, y = position.Y }
                : null;

        return JsonSerializer.Serialize(new
        {
            type = "state",
            you = you.ToString(),
            world = _room.World,
            board = _room.TicTacToe is null
                ? null
                : new
                {
                    squares = _room.TicTacToe.Squares.Select(square => square?.ToString()).ToArray(),
                    next = _room.TicTacToe.Next.ToString(),
                    winner = _room.TicTacToe.Winner?.ToString(),
                    draw = _room.TicTacToe.IsDraw,
                },
            chess = _room.Chess is not { } chess
                ? null
                : new
                {
                    fen = chess.Fen,
                    white = chess.White.ToString(),
                    toMove = chess.ToMove.ToString(),
                    inCheck = chess.InCheck,
                    lastMove = chess.LastMove is { } last ? new { from = last.From, to = last.To } : null,
                    outcome = chess.Outcome is not { } outcome
                        ? null
                        : new
                        {
                            ending = outcome.Ending == ChessEnding.Checkmate ? "checkmate" : "stalemate",
                            winner = outcome.Winner?.ToString(),
                        },
                    legalMoves = chess.LegalMoves
                        .Select(move => new { from = move.From, to = move.To, promotion = move.Promotion?.ToString() })
                        .ToArray(),
                },
            players = new { A = Player(Seat.A), B = Player(Seat.B) },
        });
    }

    private static async Task Send(WebSocket socket, string json, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        try
        {
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
        }
        catch (WebSocketException)
        {
        }
    }
}
