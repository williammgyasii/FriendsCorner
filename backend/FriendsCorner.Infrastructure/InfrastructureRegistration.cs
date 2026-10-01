using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;
using FriendsCorner.Core.Engines.Games;
using FriendsCorner.Core.Managers;
using FriendsCorner.Core.Utilities;
using FriendsCorner.Infrastructure.Accessors;
using FriendsCorner.Infrastructure.Persistence;
using FriendsCorner.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FriendsCorner.Infrastructure;

public sealed record StripeSettings(
    string? SecretKey,
    string? WebhookSecret,
    string? CornerPrice,
    string? TablePrice,
    string? HousePrice);

// Pairs every Core interface with the class that does the work, and says how
// long each one lives.
public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string databaseUrl,
        CaseWriterSettings? caseWriter = null,
        StripeSettings? stripe = null)
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
        services.AddSingleton<ILetterTilesTableAccessor, LetterTilesTableAccessor>();
        services.AddSingleton<ILetterTilesRecorderAccessor>(provider =>
            new LetterTilesRecorderAccessor(provider.GetRequiredService<ILetterTilesTableAccessor>().Save));
        services.AddSingleton<IMysteryTableAccessor, MysteryTableAccessor>();
        services.AddSingleton<IMysteryRecorderAccessor>(provider =>
            new MysteryRecorderAccessor(provider.GetRequiredService<IMysteryTableAccessor>().Save));
        services.AddSingleton<IGameRecorderAccessor, GameRecorderAccessor>();
        services.AddSingleton<IGameTableAccessor, GameTableAccessor>();
        services.AddSingleton<IRoomRegistryManager, RoomRegistryManager>();
        services.AddSingleton<IRoomManagerFactory, RoomManagerFactory>();
        services.AddHostedService<RecorderFlushAccessor>();

        // One long-lived client; the pooled lifetime picks up DNS changes.
        services.AddSingleton<ICaseWriterAccessor>(new OpenAiCaseWriterAccessor(
            new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(15) }),
            caseWriter ?? new CaseWriterSettings(ApiKey: null)));

        services.AddSingleton<IUserAccessor, UserAccessor>();
        services.AddSingleton<IHostPlanAccessor, HostPlanAccessor>();
        services.AddSingleton<IStripeAccessor>(new StripeBillingAccessor(stripe?.SecretKey, stripe?.WebhookSecret));
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<AccountManager>();
        services.AddSingleton(provider => new SubscriptionManager(
            provider.GetRequiredService<IHostPlanAccessor>(),
            provider.GetRequiredService<IStripeAccessor>(),
            stripe?.SecretKey,
            stripe?.CornerPrice,
            stripe?.TablePrice,
            stripe?.HousePrice));
        services.AddSingleton<IWordListAccessor, WordListAccessor>();
        services.AddSingleton(provider =>
            new GameCatalog(provider.GetRequiredService<IWordListAccessor>(), () => new Random()));

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
