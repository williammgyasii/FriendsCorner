using FriendsCorner.Api.Controllers;
using FriendsCorner.Infrastructure;
using FriendsCorner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FriendsCorner.Tests;

internal static class TestDatabase
{
    // Test classes run in parallel; they share one migrated schema.
    private static readonly Lazy<Task<IDbContextFactory<FriendsCornerDb>>> Migrated = new(async () =>
    {
        var provider = new ServiceCollection().AddInfrastructure(ConnectionString()).BuildServiceProvider();
        await provider.MigrateDatabaseAsync();
        return provider.GetRequiredService<IDbContextFactory<FriendsCornerDb>>();
    });

    public static Task<IDbContextFactory<FriendsCornerDb>> Contexts() => Migrated.Value;

    // Same sources the API uses in Development: its user secrets, then an
    // environment variable (ConnectionStrings__FriendsCorner) for CI.
    public static string ConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(RoomsController).Assembly)
            .AddEnvironmentVariables()
            .Build();
        var databaseUrl = configuration.GetConnectionString("FriendsCorner");
        return string.IsNullOrWhiteSpace(databaseUrl)
            ? throw new InvalidOperationException("ConnectionStrings:FriendsCorner is missing.")
            : databaseUrl;
    }
}
