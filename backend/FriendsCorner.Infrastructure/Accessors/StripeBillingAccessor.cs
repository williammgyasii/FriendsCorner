using FriendsCorner.Core.Accessors;
using Stripe;
using CheckoutSession = Stripe.Checkout.Session;
using CheckoutSessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using CheckoutSessionLineItemOptions = Stripe.Checkout.SessionLineItemOptions;
using CheckoutSessionService = Stripe.Checkout.SessionService;
using CheckoutSubscriptionData = Stripe.Checkout.SessionSubscriptionDataOptions;
using SessionManagedPaymentsOptions = Stripe.Checkout.SessionManagedPaymentsOptions;
using PortalSessionCreateOptions = Stripe.BillingPortal.SessionCreateOptions;
using PortalSessionService = Stripe.BillingPortal.SessionService;

namespace FriendsCorner.Infrastructure.Accessors;

public readonly record struct CheckoutDraft(string Mode, string PriceId, bool ChargesTax, bool ChoosesPaymentMethod, bool ManagedPayments);

// The only type that talks to Stripe. Callers see a URL or a notice.
public sealed class StripeBillingAccessor(string? secretKey, string? webhookSecret) : IStripeAccessor
{
    public const string ReturnUrl = "https://play.friendscorner.app";

    public static CheckoutDraft Describe(string priceId)
    {
        var options = SessionOptions(Guid.Empty, null, priceId, "corner", ReturnUrl);
        return new CheckoutDraft(
            options.Mode,
            options.LineItems.Single().Price,
            options.AutomaticTax is not null,
            options.PaymentMethodTypes is not null,
            options.ManagedPayments?.Enabled == true);
    }

    public async Task<string> Checkout(Guid userId, string? customerId, string priceId, string plan, string returnUrl)
    {
        var service = new CheckoutSessionService(Client());
        var session = await service.CreateAsync(SessionOptions(userId, customerId, priceId, plan, returnUrl));
        return session.Url;
    }

    public async Task<StripeEvent?> FetchCheckout(string sessionId)
    {
        try
        {
            var service = new CheckoutSessionService(Client());
            var session = await service.GetAsync(sessionId);
            if (session.Status != "complete")
            {
                return null;
            }

            var at = new DateTimeOffset(DateTime.SpecifyKind(session.Created, DateTimeKind.Utc)).ToUnixTimeSeconds();
            return Notice(at, session.CustomerId, session.Metadata, priceId: null, ended: false);
        }
        catch (StripeException)
        {
            return null;
        }
    }

    public async Task<string> Portal(string customerId)
    {
        var service = new PortalSessionService(Client());
        var session = await service.CreateAsync(new PortalSessionCreateOptions
        {
            Customer = customerId,
            ReturnUrl = ReturnUrl,
        });
        return session.Url;
    }

    public StripeEvent? Read(string body, string signature)
    {
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return null;
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(body, signature, webhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            return null;
        }

        var at = new DateTimeOffset(DateTime.SpecifyKind(stripeEvent.Created, DateTimeKind.Utc)).ToUnixTimeSeconds();
        return stripeEvent.Data.Object switch
        {
            CheckoutSession session => Notice(at, session.CustomerId, session.Metadata, priceId: null, ended: false),
            Subscription subscription => Notice(
                at,
                subscription.CustomerId,
                subscription.Metadata,
                subscription.Items?.Data.FirstOrDefault()?.Price?.Id,
                ended: stripeEvent.Type == "customer.subscription.deleted"
                    || subscription.Status is "canceled" or "unpaid" or "incomplete_expired"),
            _ => null,
        };
    }

    private static StripeEvent Notice(long at, string? customerId, IDictionary<string, string>? metadata, string? priceId, bool ended)
    {
        Guid? userId = metadata is not null && metadata.TryGetValue("user_id", out var raw) && Guid.TryParse(raw, out var id)
            ? id
            : null;
        var plan = metadata is not null && metadata.TryGetValue("plan", out var code) ? code : null;
        return new StripeEvent(at, customerId, userId, priceId, plan, ended);
    }

    private static CheckoutSessionCreateOptions SessionOptions(Guid userId, string? customerId, string priceId, string plan, string returnUrl)
    {
        var home = returnUrl.TrimEnd('/');
        return new CheckoutSessionCreateOptions
        {
            Mode = "subscription",
            Customer = customerId,
            SuccessUrl = $"{home}?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = home,
            LineItems = [new CheckoutSessionLineItemOptions { Price = priceId, Quantity = 1 }],
            Metadata = new Dictionary<string, string>
            {
                ["user_id"] = userId.ToString(),
                ["plan"] = plan,
            },
            SubscriptionData = new CheckoutSubscriptionData
            {
                Metadata = new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(),
                    ["plan"] = plan,
                },
            },
            ManagedPayments = new SessionManagedPaymentsOptions { Enabled = false },
        };
    }

    private IStripeClient Client() => new StripeClient(secretKey);
}
