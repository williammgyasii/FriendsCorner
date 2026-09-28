using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;
using Microsoft.Extensions.Hosting;

namespace FriendsCorner.Infrastructure.Accessors;

// Holds shutdown open until the last noted boards reach the database.
public sealed class RecorderFlushAccessor : BackgroundService
{
    private readonly IBoardRecorderAccessor _boards;
    private readonly IChessRecorderAccessor _chess;

    public RecorderFlushAccessor(IBoardRecorderAccessor boards, IChessRecorderAccessor chess)
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
