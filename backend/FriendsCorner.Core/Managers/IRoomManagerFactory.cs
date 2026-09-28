namespace FriendsCorner.Core.Managers;

// Rooms come and go while the app runs, so something outside Core builds them.
public interface IRoomManagerFactory
{
    IRoomManager Create(string roomId, Action onEmpty);
}
