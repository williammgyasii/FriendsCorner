using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;
using FriendsCorner.Core.Engines.LetterTiles;
using FriendsCorner.Core.Engines.Mystery;

namespace FriendsCorner.Infrastructure.Accessors;

// Keeps only the latest snapshot per room and writes it off the game's path.
public abstract class SnapshotRecorderAccessor<TBoard> : ISnapshotRecorderAccessor<TBoard>
    where TBoard : class
{
    private readonly Func<string, TBoard, Task>? _write;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, TBoard> _waiting = new();
    private Task _idle = Task.CompletedTask;
    private bool _draining;

    protected SnapshotRecorderAccessor()
    {
    }

    protected SnapshotRecorderAccessor(Func<string, TBoard, Task> write) => _write = write;

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

public sealed class BoardRecorderAccessor : SnapshotRecorderAccessor<Board>, IBoardRecorderAccessor
{
    public BoardRecorderAccessor()
    {
    }

    public BoardRecorderAccessor(Func<string, Board, Task> write)
        : base(write)
    {
    }
}

public sealed class ChessRecorderAccessor : SnapshotRecorderAccessor<ChessBoard>, IChessRecorderAccessor
{
    public ChessRecorderAccessor()
    {
    }

    public ChessRecorderAccessor(Func<string, ChessBoard, Task> write)
        : base(write)
    {
    }
}

public sealed class LetterTilesRecorderAccessor : SnapshotRecorderAccessor<LetterTilesState>, ILetterTilesRecorderAccessor
{
    public LetterTilesRecorderAccessor()
    {
    }

    public LetterTilesRecorderAccessor(Func<string, LetterTilesState, Task> write)
        : base(write)
    {
    }
}

public sealed class MysteryRecorderAccessor : SnapshotRecorderAccessor<MysteryState>, IMysteryRecorderAccessor
{
    public MysteryRecorderAccessor()
    {
    }

    public MysteryRecorderAccessor(Func<string, MysteryState, Task> write)
        : base(write)
    {
    }
}
