using FriendsCorner.Api;
using FriendsCorner.Infrastructure;
using FriendsCorner.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
// The value is a secret: user secrets locally, ConnectionStrings__FriendsCorner in production.
var databaseUrl = builder.Configuration.GetConnectionString("FriendsCorner");
if (string.IsNullOrWhiteSpace(databaseUrl))
{
    throw new InvalidOperationException("ConnectionStrings:FriendsCorner is missing.");
}

builder.Services.AddInfrastructure(databaseUrl).AddApi();
builder.Services.AddControllers();

var app = builder.Build();
await app.Services.MigrateDatabaseAsync();
app.UseWebSockets();
app.MapControllers();
app.Run();
