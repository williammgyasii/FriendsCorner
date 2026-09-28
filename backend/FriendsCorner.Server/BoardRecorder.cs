using FriendsCorner;
using Microsoft.Extensions.Hosting;

namespace FriendsCorner.Server;

public sealed class BoardRecorder
{
    private readonly Func<string, Board, Task>? _write;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Board> _waiting = new();
    private Task _idle = Task.CompletedTask;
    private bool _draining;

    public BoardRecorder()
    {
    }

    public BoardRecorder(Func<string, Board, Task> write) => _write = write;

    public void Note(string roomId, Board board)
    {
        lock (_gate)
        {
            _waiting[roomId] = board;
        }

        Kick();
    }

    public IReadOnlyList<(string RoomId, Board Board)> TakeWaiting()
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

    public Task ShowThenRecord(string roomId, Room room, Action show)
    {
        show();
        if (room.TicTacToe is { } board)
        {
            Note(roomId, board);
        }

        return Task.CompletedTask;
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

public sealed class BoardRecordingService : BackgroundService
{
    private readonly BoardRecorder _recorder;

    public BoardRecordingService(BoardRecorder recorder) => _recorder = recorder;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }

        await _recorder.WhenQuiet();
    }
}
