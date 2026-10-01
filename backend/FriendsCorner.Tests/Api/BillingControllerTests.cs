using System.Security.Claims;
using System.Text;
using FriendsCorner.Api;
using FriendsCorner.Api.Controllers;
using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Managers;
using FriendsCorner.Infrastructure;
using FriendsCorner.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FriendsCorner.Tests;

public class BillingControllerTests : IDisposable
{
    private const string CornerPrice = "price_corner";
    private const string TablePrice = "price_table";
    private const string HousePrice = "price_house";

    private readonly ServiceProvider _services = new ServiceCollection()
        .AddLogging()
        .AddInfrastructure(TestDatabase.ConnectionString())
        .AddApi("test-ticket-key")
        .BuildServiceProvider();

    private readonly ScriptedStripe _stripe = new();

    public BillingControllerTests() => _services.MigrateDatabaseAsync().GetAwaiter().GetResult();

    public void Dispose() => _services.Dispose();

    [Fact]
    public async Task Checkout_portal_and_the_webhook_record_the_plan()
    {
        var plans = _services.GetRequiredService<IHostPlanAccessor>();
        var billing = new SubscriptionManager(plans, _stripe, "sk_test", CornerPrice, TablePrice, HousePrice);
        var manager = _services.GetRequiredService<AccountManager>();
        var accounts = new AccountsController(manager, billing);
        var email = $"ada.{Guid.NewGuid():N}@x.com";
        var created = await manager.Register("Ada", "Countess", email, "correct-horse");
        var userId = created.Id!.Value;
        var host = Bills(billing, userId);
        var stranger = Bills(billing, null);

        Assert.IsType<UnauthorizedResult>(await stranger.Post(new BillingRequest("checkout", "table")));
        Assert.IsType<BadRequestResult>(await host.Post(new BillingRequest("checkout", "weekly")));
        Assert.IsType<NotFoundResult>(await host.Post(new BillingRequest("portal", null)));

        var unconfigured = new SubscriptionManager(plans, new ScriptedStripe(), null, CornerPrice, TablePrice, HousePrice);
        Assert.Equal(503, Assert.IsType<StatusCodeResult>(await Bills(unconfigured, userId).Post(new BillingRequest("checkout", "corner"))).StatusCode);

        var link = Link(await host.Post(new BillingRequest("checkout", "corner")));
        Assert.StartsWith("https://", link.Url);
        Assert.Null(Body(await Account(accounts, userId).Current()).Plan);

        _stripe.Next = null;
        Assert.IsType<BadRequestResult>(await Webhook(host, "bad"));
        Assert.Null(Body(await Account(accounts, userId).Current()).Plan);

        var customer = $"cus_{userId:N}";
        _stripe.Next = new StripeEvent(100, customer, userId, TablePrice, null, false);
        Assert.IsType<OkResult>(await Webhook(host, "signed"));
        var table = Body(await Account(accounts, userId).Current());
        Assert.Equal(userId, table.Id);
        Assert.Equal("table", table.Plan);

        await Webhook(host, "signed");
        Assert.Equal(customer, (await plans.Find(userId))!.CustomerId);

        Assert.IsType<ConflictResult>(await host.Post(new BillingRequest("checkout", "corner")));

        _stripe.Next = new StripeEvent(300, customer, userId, CornerPrice, null, false);
        await Webhook(host, "signed");
        Assert.Equal("corner", Body(await Account(accounts, userId).Current()).Plan);

        _stripe.Next = new StripeEvent(200, customer, userId, TablePrice, null, false);
        await Webhook(host, "signed");
        Assert.Equal("corner", Body(await Account(accounts, userId).Current()).Plan);

        _stripe.Next = new StripeEvent(400, customer, userId, null, null, true);
        await Webhook(host, "signed");
        Assert.Null(Body(await Account(accounts, userId).Current()).Plan);
        Assert.StartsWith("https://", Link(await host.Post(new BillingRequest("portal", null))).Url);

        _stripe.CheckoutNotice = new StripeEvent(500, customer, userId, null, "corner", false);
        Assert.IsType<NoContentResult>(await host.Post(new BillingRequest("confirm", SessionId: "cs_test")));
        Assert.Equal("corner", Body(await Account(accounts, userId).Current()).Plan);
    }

    private BillingController Bills(SubscriptionManager billing, Guid? userId)
    {
        var controller = new BillingController(billing, _stripe);
        var http = new DefaultHttpContext();
        if (userId is Guid id)
        {
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "Cookies");
            http.User = new ClaimsPrincipal(identity);
        }

        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    private static AccountsController Account(AccountsController accounts, Guid userId)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, "ignored@x.com"),
            ],
            "Cookies");
        accounts.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
        return accounts;
    }

    private static async Task<IActionResult> Webhook(BillingController controller, string signature)
    {
        controller.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        controller.HttpContext.Request.Headers["Stripe-Signature"] = signature;
        return await controller.Webhook();
    }

    private static BillingLink Link(IActionResult result) =>
        Assert.IsType<BillingLink>(Assert.IsType<OkObjectResult>(result).Value);

    private static AccountView Body(IActionResult result) =>
        Assert.IsType<AccountView>(Assert.IsType<OkObjectResult>(result).Value);

    private sealed class ScriptedStripe : IStripeAccessor
    {
        public StripeEvent? Next { get; set; }
        public StripeEvent? CheckoutNotice { get; set; }

        public Task<string> Checkout(Guid userId, string? customerId, string priceId, string plan, string returnUrl) =>
            Task.FromResult("https://checkout.stripe.com/c/pay/cs_test");

        public Task<string> Portal(string customerId) =>
            Task.FromResult("https://billing.stripe.com/p/session/test");

        public StripeEvent? Read(string body, string signature) => signature == "signed" ? Next : null;

        public Task<StripeEvent?> FetchCheckout(string sessionId) =>
            Task.FromResult(sessionId == "cs_test" ? CheckoutNotice : null);
    }
}
