using FriendsCorner.Infrastructure;
using FriendsCorner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FriendsCorner.Tests;

public class FriendsCornerDbTests
{
    [Fact]
    public async Task The_row_classes_match_the_latest_migration()
    {
        await using var db = await (await TestDatabase.Contexts()).CreateDbContextAsync();

        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Migrating_at_startup_leaves_nothing_pending_in_Neon()
    {
        await using var provider = new ServiceCollection()
            .AddInfrastructure(TestDatabase.ConnectionString())
            .BuildServiceProvider();

        await provider.MigrateDatabaseAsync();

        await using var db = await provider
            .GetRequiredService<IDbContextFactory<FriendsCornerDb>>()
            .CreateDbContextAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
    }
}
