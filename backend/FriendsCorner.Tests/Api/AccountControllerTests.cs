using System.Text.Json;
using FriendsCorner.Api;
using FriendsCorner.Api.Controllers;
using FriendsCorner.Infrastructure;
using FriendsCorner.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FriendsCorner.Tests;

public class AccountControllerTests : IDisposable
{
    private const string TicketKey = "test-ticket-key";
    private const string Password = "correct-horse";

    private readonly ServiceProvider _services = new ServiceCollection()
        .AddLogging()
        .AddInfrastructure(TestDatabase.ConnectionString())
        .AddApi(TicketKey)
        .BuildServiceProvider();

    private readonly string _email = $"ada.{Guid.NewGuid():N}@x.com";

    public AccountControllerTests() => _services.MigrateDatabaseAsync().GetAwaiter().GetResult();

    public void Dispose() => _services.Dispose();

    [Fact]
    public void A_missing_ticket_key_is_refused()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddApi(null));
        Assert.Throws<InvalidOperationException>(() => services.AddApi(" "));
    }

    [Fact]
    public async Task A_host_registers_signs_in_and_signs_out()
    {
        var context = Context();
        var accounts = Controller(context);
        var mixedCase = " " + _email.ToUpperInvariant();

        var created = await accounts.Post(new AccountRequest("register", mixedCase, Password, "Ada Lovelace", "Countess"));
        var view = Body(created);
        Assert.Equal(_email, view.Email);
        Assert.Equal("Ada Lovelace", view.Name);
        Assert.Equal("Countess", view.GameName);
        Assert.DoesNotContain(Password, JsonSerializer.Serialize(view));
        Assert.Contains("friends.session", context.Response.Headers.SetCookie.ToString());

        var session = await WithSession(context.Response.Headers.SetCookie.ToString());
        var current = Body(await session.Current());
        Assert.Equal(view.Id, current.Id);
        Assert.Equal(_email, current.Email);
        Assert.Equal("Countess", current.GameName);

        Assert.IsType<ConflictResult>(await accounts.Post(new AccountRequest("register", _email, "another-password", "Ada", "Other")));
        Assert.Equal(view.Id, Body(await session.Current()).Id);

        Assert.Equal("password", Error(await accounts.Post(new AccountRequest("register", $"new.{Guid.NewGuid():N}@x.com", "short", "Ada", "Countess"))));
        Assert.Equal("name", Error(await accounts.Post(new AccountRequest("register", $"long.{Guid.NewGuid():N}@x.com", Password, new string('a', 41), "Countess"))));
        Assert.Equal("gameName", Error(await accounts.Post(new AccountRequest("register", $"blank.{Guid.NewGuid():N}@x.com", Password, "Ada", "   "))));

        var wrong = await accounts.Post(new AccountRequest("login", _email, "wrong-password"));
        var unknown = await accounts.Post(new AccountRequest("login", $"nobody.{Guid.NewGuid():N}@x.com", Password));
        Assert.Equal(wrong.GetType(), unknown.GetType());
        var rejected = Assert.IsType<ContentResult>(wrong);
        Assert.Equal(StatusCodes.Status401Unauthorized, rejected.StatusCode);
        Assert.Equal("", rejected.Content);
        Assert.DoesNotContain(Password, JsonSerializer.Serialize(wrong));
        Assert.DoesNotContain("wrong-password", JsonSerializer.Serialize(unknown));

        var signedIn = Body(await accounts.Post(new AccountRequest("login", mixedCase, Password)));
        Assert.Equal(view.Id, signedIn.Id);
        Assert.Equal("Countess", signedIn.GameName);

        Assert.IsType<NoContentResult>(await accounts.Post(new AccountRequest("logout", null, null)));
        var signedOut = Assert.IsType<ContentResult>(await Controller(Context()).Current());
        Assert.Equal(StatusCodes.Status401Unauthorized, signedOut.StatusCode);
        Assert.Equal("", signedOut.Content);
    }

    private async Task<AccountsController> WithSession(string setCookie)
    {
        var context = Context();
        context.Request.Headers.Cookie = setCookie.Split(';')[0];
        var result = await context.RequestServices
            .GetRequiredService<IAuthenticationService>()
            .AuthenticateAsync(context, CookieAuthenticationDefaults.AuthenticationScheme);
        Assert.True(result.Succeeded, result.Failure?.Message);
        context.User = result.Principal!;
        return Controller(context);
    }

    [Fact]
    public void Two_keys_that_match_read_each_others_ticket()
    {
        var format = new ConfiguredTicketFormat(TicketKey);
        var other = new ConfiguredTicketFormat(TicketKey);
        var identity = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "ada@x.com"),
            ],
            "Cookies");
        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(
            new System.Security.Claims.ClaimsPrincipal(identity),
            new Microsoft.AspNetCore.Authentication.AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1) },
            "Cookies");

        var restored = other.Unprotect(format.Protect(ticket));

        Assert.Equal(
            ticket.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value,
            restored!.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    }

    private AccountsController Controller(HttpContext context)
    {
        var controller = ActivatorUtilities.CreateInstance<AccountsController>(_services);
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    private DefaultHttpContext Context()
    {
        // A scope per request. The cookie handler is cached on the scope, so two requests cannot share one.
        var context = new DefaultHttpContext { RequestServices = _services.CreateScope().ServiceProvider };
        context.Request.Scheme = "https";
        return context;
    }

    private static AccountView Body(IActionResult result) =>
        Assert.IsType<AccountView>(Assert.IsType<OkObjectResult>(result).Value);

    private static string Error(IActionResult result) =>
        Assert.IsType<AccountProblem>(Assert.IsType<BadRequestObjectResult>(result).Value).Error;
}
