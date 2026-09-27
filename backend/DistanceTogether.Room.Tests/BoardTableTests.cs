using DistanceTogether.Storage;

namespace DistanceTogether.Tests;

public class BoardTableTests
{
    [Fact]
    public async Task An_empty_board_is_nine_empty_squares_when_read_back()
    {
        var table = new BoardTable(ConnectionString());
        var roomId = Guid.NewGuid().ToString("N");

        await table.Save(roomId, Board.Empty());
        var loaded = await table.Load(roomId);

        Assert.NotNull(loaded);
        Assert.Equal(9, loaded.Squares.Count);
        Assert.All(loaded.Squares, square => Assert.Null(square));
        Assert.Equal(Seat.A, loaded.Next);

        await table.Remove(roomId);
    }

    [Fact]
    public async Task A_move_is_still_there_when_the_board_is_read_back()
    {
        var table = new BoardTable(ConnectionString());
        var roomId = Guid.NewGuid().ToString("N");
        Assert.True(Board.Empty().TryPlace(Seat.A, 4, out var played));

        await table.Save(roomId, played);
        var loaded = await table.Load(roomId);

        Assert.NotNull(loaded);
        Assert.Equal('X', loaded.Squares[4]);
        Assert.Equal(Seat.B, loaded.Next);

        await table.Remove(roomId);
    }

    private static string ConnectionString()
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
