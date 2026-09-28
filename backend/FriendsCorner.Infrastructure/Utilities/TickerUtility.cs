using FriendsCorner.Core.Utilities;

namespace FriendsCorner.Infrastructure.Utilities;

// Calls back on a fixed period from the first Start until Stop. One per room.
public sealed class TickerUtility : ITickerUtility
{
    private readonly CancellationTokenSource _stopping = new();
    private int _started;

    public void Start(TimeSpan period, Func<Task> onTick)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(period);
            try
            {
                while (await timer.WaitForNextTickAsync(_stopping.Token))
                {
                    await onTick();
                }
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    public void Stop() => _stopping.Cancel();
}
