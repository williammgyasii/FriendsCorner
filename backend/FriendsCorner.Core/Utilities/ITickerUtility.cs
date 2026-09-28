namespace FriendsCorner.Core.Utilities;

// Calls back on a fixed period from the first Start until Stop.
public interface ITickerUtility
{
    void Start(TimeSpan period, Func<Task> onTick);

    void Stop();
}
