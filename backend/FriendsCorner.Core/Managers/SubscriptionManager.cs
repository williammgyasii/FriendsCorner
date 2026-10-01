using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;

namespace FriendsCorner.Core.Managers;

public enum BillingStatus
{
    Checkout,
    Portal,
    UnknownPlan,
    AlreadySubscribed,
    NoCustomer,
    NotConfigured,
    Confirmed,
    SessionNotFound,
    SessionMismatch,
}

public readonly record struct BillingResult(BillingStatus Status, string? Url);

// Checkout, the portal, and applying a webhook. The engine decides; this orders the steps.
public sealed class SubscriptionManager(
    IHostPlanAccessor plans,
    IStripeAccessor stripe,
    string? secretKey,
    string? cornerPrice,
    string? tablePrice,
    string? housePrice)
{
    public const string DefaultReturnUrl = "https://play.friendscorner.app";

    public async Task<BillingResult> Checkout(Guid userId, string? plan, string? returnUrl)
    {
        if (!SubscriptionEngine.TryPlan(plan, out _))
        {
            return new BillingResult(BillingStatus.UnknownPlan, null);
        }

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return new BillingResult(BillingStatus.NotConfigured, null);
        }

        var current = await plans.Find(userId);
        if (current?.Plan is not null)
        {
            return new BillingResult(BillingStatus.AlreadySubscribed, null);
        }

        var url = await stripe.Checkout(userId, current?.CustomerId, PriceId(plan!), plan!, returnUrl ?? DefaultReturnUrl);
        return new BillingResult(BillingStatus.Checkout, url);
    }

    public async Task<BillingResult> ConfirmCheckout(Guid userId, string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return new BillingResult(BillingStatus.SessionNotFound, null);
        }

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return new BillingResult(BillingStatus.NotConfigured, null);
        }

        var notice = await stripe.FetchCheckout(sessionId);
        if (notice is null)
        {
            return new BillingResult(BillingStatus.SessionNotFound, null);
        }

        if (notice.Value.UserId != userId)
        {
            return new BillingResult(BillingStatus.SessionMismatch, null);
        }

        await Apply(notice.Value);
        return new BillingResult(BillingStatus.Confirmed, null);
    }

    public async Task<BillingResult> Portal(Guid userId)
    {
        var current = await plans.Find(userId);
        if (current?.CustomerId is null)
        {
            return new BillingResult(BillingStatus.NoCustomer, null);
        }

        var url = await stripe.Portal(current.CustomerId);
        return new BillingResult(BillingStatus.Portal, url);
    }

    public async Task Apply(StripeEvent notice)
    {
        var userId = notice.UserId ?? (notice.CustomerId is null ? null : (await plans.FindByCustomer(notice.CustomerId))?.UserId);
        if (userId is not Guid id)
        {
            return;
        }

        var row = await plans.Find(id);
        var current = row is null
            ? new HostBilling(null, null, 0)
            : new HostBilling(Parse(row.Plan), row.CustomerId, row.EventAt);
        PlanCode? plan = notice.Ended ? null : Price(notice.PriceId) ?? Parse(notice.Plan);
        if (!notice.Ended && plan is null)
        {
            return;
        }

        var update = SubscriptionEngine.Apply(current, new BillingNotice(notice.At, notice.CustomerId, plan));
        if (!update.Applied)
        {
            return;
        }

        await plans.Save(new HostPlanRow(id, update.CustomerId, Code(update.Plan), update.At));
    }

    public async Task<string?> Plan(Guid userId) => (await plans.Find(userId))?.Plan;

    private string PriceId(string plan) => plan switch
    {
        "table" => tablePrice ?? "",
        "house" => housePrice ?? "",
        _ => cornerPrice ?? "",
    };

    private PlanCode? Price(string? priceId)
    {
        if (priceId is null)
        {
            return null;
        }

        if (priceId == cornerPrice)
        {
            return PlanCode.Corner;
        }

        if (priceId == tablePrice)
        {
            return PlanCode.Table;
        }

        if (priceId == housePrice)
        {
            return PlanCode.House;
        }

        return null;
    }

    private static PlanCode? Parse(string? plan) =>
        SubscriptionEngine.TryPlan(plan, out var code) ? code : null;

    private static string? Code(PlanCode? plan) => plan switch
    {
        PlanCode.Corner => "corner",
        PlanCode.Table => "table",
        PlanCode.House => "house",
        _ => null,
    };
}
