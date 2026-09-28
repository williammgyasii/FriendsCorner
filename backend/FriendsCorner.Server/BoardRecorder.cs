using FriendsCorner;
using Microsoft.Extensions.Hosting;

namespace FriendsCorner.Server;

// Keeps only the latest snapshot per room and writes it off the game's path.
public abstract class SnapshotRecorder<TBoard>
    where TBoard : class
{
    private readonly Func<string, TBoard, Task>? _write;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, TBoard> _waiting = new();
    private Task _idle = Task.CompletedTask;
    private bool _draining;

    protected SnapshotRecorder()
    {
    }

    protected SnapshotRecorder(Func<string, TBoard, Task> write) => _write = write;

    public void Note(string roomId, TBoard board)
    {
        lock (_gate)
        {
            _waiting[roomId] = board;
        }

        Kick();
    }

    public IReadOnlyList<(string RoomId, TBoard Board)> TakeWaiting()
    {
        lock (_gate)
        {
            var taken = _waiting
                .Select(pair => (RoomId: pair.Key, Board: pair.Value))
                .ToArray();
            _waiting.Clear();
            return taken;
        }
    }

    public Task WhenQuiet() => FlushAsync();

    private void Kick()
    {
        if (_write is null)
        {
            return;
        }

        lock (_gate)
        {
            if (_draining)
            {
                return;
            }

            _draining = true;
            _idle = DrainAsync();
        }
    }

    private async Task DrainAsync()
    {
        try
        {
            while (true)
            {
                var batch = TakeWaiting();
                if (batch.Count == 0)
                {
                    return;
                }

                foreach (var snapshot in batch)
                {
                    await _write!(snapshot.RoomId, snapshot.Board);
                }
            }
        }
        finally
        {
            var more = false;
            lock (_gate)
            {
                _draining = false;
                more = _waiting.Count > 0;
            }

            if (more)
            {
                Kick();
            }
        }
    }

    private async Task FlushAsync()
    {
        while (true)
        {
            Task idle;
            lock (_gate)
            {
                idle = _idle;
            }

            await idle;

            lock (_gate)
            {
                if (!_draining && _waiting.Count == 0)
                {
                    return;
                }
            }
        }
    }
}

public sealed class BoardRecorder : SnapshotRecorder<Board>
{
    public BoardRecorder()
    {
    }

    public BoardRecorder(Func<string, Board, Task> write)
        : base(write)
    {
    }

    public Task ShowThenRecord(string roomId, Room room, Action show)
    {
        show();
        if (room.TicTacToe is { } board)
        {
            Note(roomId, board);
        }

        return Task.CompletedTask;
    }
}

public sealed class ChessRecorder : SnapshotRecorder<ChessBoard>
{
    public ChessRecorder()
    {
    }

    public ChessRecorder(Func<string, ChessBoard, Task> write)
        : base(write)
    {
    }
}

public sealed class BoardRecordingService : BackgroundService
{
    private readonly BoardRecorder _boards;
    private readonly ChessRecorder _chess;

    public BoardRecordingService(BoardRecorder boards, ChessRecorder chess)
    {
        _boards = boards;
        _chess = chess;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }

        await Task.WhenAll(_boards.WhenQuiet(), _chess.WhenQuiet());
    }
}
