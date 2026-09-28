using FriendsCorner.Core.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace FriendsCorner.Infrastructure;

// Rooms come and go while the app runs, so they cannot be singletons. The
// container still builds each one, with its own engine, sockets, and clock.
public sealed class RoomManagerFactory : IRoomManagerFactory
{
    private readonly IServiceProvider _services;

    public RoomManagerFactory(IServiceProvider services) => _services = services;

    public IRoomManager Create(string roomId, Action onEmpty) =>
        ActivatorUtilities.CreateInstance<RoomManager>(_services, roomId, onEmpty);
}
