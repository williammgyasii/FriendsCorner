using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;
using FriendsCorner.Core.Managers;
using FriendsCorner.Core.Utilities;
using FriendsCorner.Infrastructure.Accessors;
using FriendsCorner.Infrastructure.Persistence;
using FriendsCorner.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FriendsCorner.Infrastructure;

// Pairs every Core interface with the class that does the work, and says how
// long each one lives.
public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string databaseUrl)
    {
        // One per app: shared state or a shared connection string.
        services.AddSingleton(TimeProvider.System);
        // A DbContext is short-lived and not thread-safe; singletons hold the
        // factory and open one context per call.
        services.AddDbContextFactory<FriendsCornerDb>(options =>
            options.UseNpgsql(NeonConnection.ToNpgsql(databaseUrl)));
        services.AddSingleton<IBoardTableAccessor, BoardTableAccessor>();
        services.AddSingleton<IChessTableAccessor, ChessTableAccessor>();
        services.AddSingleton<IBoardRecorderAccessor>(provider =>
            new BoardRecorderAccessor(provider.GetRequiredService<IBoardTableAccessor>().Save));
        services.AddSingleton<IChessRecorderAccessor>(provider =>
            new ChessRecorderAccessor(provider.GetRequiredService<IChessTableAccessor>().Save));
        services.AddSingleton<IRoomRegistryManager, RoomRegistryManager>();
        services.AddSingleton<IRoomManagerFactory, RoomManagerFactory>();
        services.AddHostedService<RecorderFlushAccessor>();

        // Stateless helpers: one copy is enough.
        services.AddSingleton<IStateMessageAccessor, StateMessageAccessor>();
        services.AddSingleton<IWebSocketTextUtility, WebSocketTextUtility>();

        // A fresh one for every room the factory builds.
        services.AddTransient<RoomEngine>();
        services.AddTransient<ISeatSocketAccessor, SeatSocketAccessor>();
        services.AddTransient<ITickerUtility, TickerUtility>();

        return services;
    }
}
