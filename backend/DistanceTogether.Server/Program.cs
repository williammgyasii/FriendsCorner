using DistanceTogether.Server;
using DistanceTogether.Storage;

LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? throw new InvalidOperationException("DATABASE_URL is missing.");
builder.Services.AddSingleton(new BoardTable(databaseUrl));
builder.Services.AddSingleton(services => new BoardRecorder(services.GetRequiredService<BoardTable>().Save));
builder.Services.AddHostedService<BoardRecordingService>();
builder.Services.AddSingleton<RoomRegistry>();

var app = builder.Build();
app.UseWebSockets();

app.MapPost("/rooms", (RoomRegistry registry) => Results.Json(new { id = registry.Create() }));

app.Map("/ws/{roomId}", async (HttpContext context, string roomId, RoomRegistry registry) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var session = await registry.Find(roomId);
    if (session is null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await session.Join(socket, context.RequestAborted);
});

app.Run();

static void LoadDotEnv()
{
    if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DATABASE_URL")))
    {
        return;
    }

    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        var env = Path.Combine(directory.FullName, ".env");
        if (File.Exists(env))
        {
            foreach (var line in File.ReadAllLines(env))
            {
                const string prefix = "DATABASE_URL=";
                if (line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    Environment.SetEnvironmentVariable("DATABASE_URL", line[prefix.Length..]);
                    return;
                }
            }
        }

        directory = directory.Parent;
    }
}
