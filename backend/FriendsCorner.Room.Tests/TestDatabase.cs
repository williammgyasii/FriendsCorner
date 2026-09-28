namespace FriendsCorner.Tests;

internal static class TestDatabase
{
    public static string ConnectionString()
    {
        var existing = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var env = Path.Combine(directory.FullName, ".env");
            if (File.Exists(env))
            {
                foreach (var line in File.ReadAllLines(env))
                {
                    if (line.StartsWith("DATABASE_URL=", StringComparison.Ordinal))
                    {
                        return line["DATABASE_URL=".Length..];
                    }
                }
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("DATABASE_URL is missing.");
    }
}
