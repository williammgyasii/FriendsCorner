using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;
namespace FriendsCorner.Infrastructure.Accessors;

internal static class NeonConnection
{
    public static string ToNpgsql(string connectionString)
    {
        if (!connectionString.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port > 0 ? uri.Port : 5432;
        return $"Host={uri.Host};Port={port};Username={user};Password={password};Database={database};SSL Mode=Require";
    }
}
