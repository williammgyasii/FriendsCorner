using System.Security.Claims;
using FriendsCorner.Core.Managers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace FriendsCorner.Api.Controllers;

public sealed record AccountRequest(string Action, string? Email, string? Password, string? Name = null, string? GameName = null);

public sealed record AccountView(Guid Id, string Email, string Name, string GameName, string? Plan = null);

public sealed record AccountProblem(string Error);

[ApiController]
[Route("account")]
public sealed class AccountsController(AccountManager accounts, SubscriptionManager billing) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Current()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (id is null || email is null || !Guid.TryParse(id, out var userId))
        {
            return Rejected();
        }

        var name = User.FindFirst(ClaimTypes.Name)?.Value ?? email.Split('@')[0];
        var gameName = User.FindFirst(GameNameClaim)?.Value ?? name;
        return Ok(new AccountView(userId, email, name, gameName, await billing.Plan(userId)));
    }

    [HttpPost]
    public async Task<IActionResult> Post(AccountRequest request)
    {
        switch (request.Action)
        {
            case "register":
                return await Register(request.Name ?? "", request.GameName ?? "", request.Email ?? "", request.Password ?? "");
            case "login":
                return await Login(request.Email ?? "", request.Password ?? "");
            case "logout":
                await HttpContext.SignOutAsync();
                return NoContent();
            default:
                return BadRequest();
        }
    }

    private async Task<IActionResult> Register(string name, string gameName, string email, string password)
    {
        var result = await accounts.Register(name, gameName, email, password);
        return result.Status switch
        {
            AccountStatus.TooShort => BadRequest(new AccountProblem("password")),
            AccountStatus.InvalidName => BadRequest(new AccountProblem("name")),
            AccountStatus.InvalidGameName => BadRequest(new AccountProblem("gameName")),
            AccountStatus.Duplicate => Conflict(),
            AccountStatus.Created => await SignedIn(result.Id!.Value, AccountEngineEmail(email), result.Name!, result.GameName!),
            _ => BadRequest(new AccountProblem("name")),
        };
    }

    private async Task<IActionResult> Login(string email, string password)
    {
        var result = await accounts.SignIn(email, password);
        return result.Status == AccountStatus.SignedIn
            ? await SignedIn(result.Id!.Value, AccountEngineEmail(email), result.Name!, result.GameName!)
            : Rejected();
    }

    // An empty body, so a wrong password and an unknown email look the same.
    private static ContentResult Rejected() => new() { StatusCode = StatusCodes.Status401Unauthorized, Content = "" };

    public const string GameNameClaim = "game_name";

    private async Task<IActionResult> SignedIn(Guid id, string email, string name, string gameName)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name),
                new Claim(GameNameClaim, gameName),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
            });
        return Ok(new AccountView(id, email, name, gameName));
    }

    private static string AccountEngineEmail(string email) =>
        FriendsCorner.Core.Engines.AccountEngine.NormalizeEmail(email);
}
