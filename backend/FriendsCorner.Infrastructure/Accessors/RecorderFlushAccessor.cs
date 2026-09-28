using FriendsCorner.Core.Accessors;
using Microsoft.Extensions.Hosting;

namespace FriendsCorner.Infrastructure.Accessors;

// Holds shutdown open until the last noted boards reach the database.
public sealed class RecorderFlushAccessor : BackgroundService
{
    private readonly IBoardRecorderAccessor _boards;
    private readonly IChessRecorderAccessor _chess;
    private readonly ILetterTilesRecorderAccessor _tiles;

    public RecorderFlushAccessor(IBoardRecorderAccessor boards, IChessRecorderAccessor chess, ILetterTilesRecorderAccessor tiles)
    {
        _boards = boards;
        _chess = chess;
        _tiles = tiles;
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

        await Task.WhenAll(_boards.WhenQuiet(), _chess.WhenQuiet(), _tiles.WhenQuiet());
    }
}
