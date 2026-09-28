using FriendsCorner.Infrastructure.Accessors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FriendsCorner.Infrastructure.Persistence;

// Lets `dotnet ef migrations add` build the model without booting the API.
// Adding a migration never connects, so the placeholder is never dialled.
public sealed class DesignTimeFriendsCornerDb : IDesignTimeDbContextFactory<FriendsCornerDb>
{
    public FriendsCornerDb CreateDbContext(string[] args)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("ConnectionStrings__FriendsCorner")
            ?? "Host=localhost;Database=friends_corner";
        return new FriendsCornerDb(new DbContextOptionsBuilder<FriendsCornerDb>()
            .UseNpgsql(NeonConnection.ToNpgsql(databaseUrl))
            .Options);
    }
}
