using FriendsCorner.Api;
using FriendsCorner.Infrastructure;
using FriendsCorner.Infrastructure.Accessors;
using FriendsCorner.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
// The value is a secret: user secrets locally, ConnectionStrings__FriendsCorner in production.
var databaseUrl = builder.Configuration.GetConnectionString("FriendsCorner");
if (string.IsNullOrWhiteSpace(databaseUrl))
{
    throw new InvalidOperationException("ConnectionStrings:FriendsCorner is missing.");
}

// The key is a secret too (OpenAI__ApiKey in production); without it a mystery fails cleanly.
var caseWriter = new CaseWriterSettings(
    builder.Configuration["OpenAI:ApiKey"],
    builder.Configuration["OpenAI:Model"] ?? CaseWriterSettings.DefaultModel,
    builder.Configuration["OpenAI:Effort"] ?? CaseWriterSettings.DefaultEffort);

var ticketKey = builder.Configuration["Auth:TicketKey"];
if (string.IsNullOrWhiteSpace(ticketKey))
{
    throw new InvalidOperationException("Auth:TicketKey is missing.");
}

var stripe = new StripeSettings(
    builder.Configuration["Stripe:SecretKey"],
    builder.Configuration["Stripe:WebhookSecret"],
    builder.Configuration["Stripe:Prices:Corner"],
    builder.Configuration["Stripe:Prices:Table"],
    builder.Configuration["Stripe:Prices:House"]);

builder.Services.AddInfrastructure(databaseUrl, caseWriter, stripe).AddApi(ticketKey);
builder.Services.AddControllers();

var app = builder.Build();
await app.Services.MigrateDatabaseAsync();
app.UseWebSockets();
app.UseAuthentication();
app.MapControllers();
app.Run();
