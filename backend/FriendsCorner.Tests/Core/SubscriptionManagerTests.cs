using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Managers;

namespace FriendsCorner.Tests;

public class SubscriptionManagerTests
{
    private const string CornerPrice = "price_corner";
    private const string TablePrice = "price_table";
    private const string HousePrice = "price_house";

    [Fact]
    public async Task Checkout_uses_stripe_only_when_the_host_can_subscribe()
    {
        var plans = new FakePlans();
        var stripe = new FakeStripe();
        var user = Guid.NewGuid();
        var billing = Manager(plans, stripe, "sk_test");

        var weekly = await billing.Checkout(user, "weekly", null);
        var missing = await new SubscriptionManager(plans, stripe, null, CornerPrice, TablePrice, HousePrice).Checkout(user, "corner", null);
        var checkout = await billing.Checkout(user, "corner", null);
        plans.Rows.Add(new HostPlanRow(user, null, "house", 1));
        var second = await billing.Checkout(user, "corner", null);
        var portal = await new SubscriptionManager(new FakePlans(), stripe, "sk_test", CornerPrice, TablePrice, HousePrice).Portal(user);

        Assert.Equal(BillingStatus.UnknownPlan, weekly.Status);
        Assert.Equal(BillingStatus.NotConfigured, missing.Status);
        Assert.Equal(BillingStatus.Checkout, checkout.Status);
        Assert.Equal("https://checkout.stripe.test/corner", checkout.Url);
        Assert.Equal(BillingStatus.AlreadySubscribed, second.Status);
        Assert.Equal(BillingStatus.NoCustomer, portal.Status);
        Assert.Equal(1, stripe.Checkouts);
    }

    [Fact]
    public async Task Confirm_checkout_records_the_plan_when_the_session_belongs_to_the_host()
    {
        var plans = new FakePlans();
        var user = Guid.NewGuid();
        var stripe = new FakeStripe
        {
            CheckoutNotice = new StripeEvent(100, "cus_1", user, null, "corner", false),
        };
        var billing = Manager(plans, stripe, "sk_test");

        var missing = await billing.ConfirmCheckout(user, null);
        var wrongHost = await billing.ConfirmCheckout(Guid.NewGuid(), "cs_test");
        var confirmed = await billing.ConfirmCheckout(user, "cs_test");

        Assert.Equal(BillingStatus.SessionNotFound, missing.Status);
        Assert.Equal(BillingStatus.SessionMismatch, wrongHost.Status);
        Assert.Equal(BillingStatus.Confirmed, confirmed.Status);
        Assert.Equal("corner", plans.Rows.Single().Plan);
    }

    [Fact]
    public async Task A_table_notice_is_stored_once()
    {
        var plans = new FakePlans();
        var user = Guid.NewGuid();
        var billing = Manager(plans, new FakeStripe(), "sk_test");
        var notice = new StripeEvent(100, "cus_1", user, TablePrice, null, false);

        await billing.Apply(notice);
        await billing.Apply(notice);

        Assert.Equal("table", plans.Rows.Single().Plan);
        Assert.Equal("cus_1", plans.Rows.Single().CustomerId);
        Assert.Equal(1, plans.Saves);
    }

    private static SubscriptionManager Manager(FakePlans plans, FakeStripe stripe, string? secret) =>
        new(plans, stripe, secret, CornerPrice, TablePrice, HousePrice);

    private sealed class FakePlans : IHostPlanAccessor
    {
        public List<HostPlanRow> Rows { get; } = [];
        public int Saves { get; private set; }

        public Task<HostPlanRow?> Find(Guid userId) =>
            Task.FromResult(Rows.SingleOrDefault(row => row.UserId == userId));

        public Task<HostPlanRow?> FindByCustomer(string customerId) =>
            Task.FromResult(Rows.SingleOrDefault(row => row.CustomerId == customerId));

        public Task Save(HostPlanRow plan)
        {
            Saves++;
            Rows.RemoveAll(row => row.UserId == plan.UserId);
            Rows.Add(plan);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStripe : IStripeAccessor
    {
        public int Checkouts { get; private set; }
        public StripeEvent? CheckoutNotice { get; set; }

        public Task<string> Checkout(Guid userId, string? customerId, string priceId, string plan, string returnUrl)
        {
            Checkouts++;
            return Task.FromResult($"https://checkout.stripe.test/{plan}");
        }

        public Task<string> Portal(string customerId) => Task.FromResult("https://billing.stripe.test/portal");

        public StripeEvent? Read(string body, string signature) => null;

        public Task<StripeEvent?> FetchCheckout(string sessionId) =>
            Task.FromResult(sessionId == "cs_test" ? CheckoutNotice : null);
    }
}
