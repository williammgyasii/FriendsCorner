using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FriendsCorner.Infrastructure.Persistence;

public static class DatabaseMigration
{
    // Safe while there is one instance. With several, run migrations as a deploy
    // step instead so two instances never race on the schema.
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var contexts = services.GetRequiredService<IDbContextFactory<FriendsCornerDb>>();
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
