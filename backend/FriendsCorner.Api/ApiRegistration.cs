using FriendsCorner.Api.Contracts;

namespace FriendsCorner.Api;

// Our own services at the web edge. MVC itself is added by the host in Program.
public static class ApiRegistration
{
    public static IServiceCollection AddApi(this IServiceCollection services) =>
        services.AddSingleton<IClientMessageReader, ClientMessageReader>();
}
