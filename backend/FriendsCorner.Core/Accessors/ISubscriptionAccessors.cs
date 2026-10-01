namespace FriendsCorner.Core.Accessors;

public sealed record HostPlanRow(Guid UserId, string? CustomerId, string? Plan, long EventAt);

public readonly record struct StripeEvent(long At, string? CustomerId, Guid? UserId, string? PriceId, string? Plan, bool Ended);

public interface IHostPlanAccessor
{
    Task<HostPlanRow?> Find(Guid userId);

    Task<HostPlanRow?> FindByCustomer(string customerId);

    Task Save(HostPlanRow plan);
}

public interface IStripeAccessor
{
    Task<string> Checkout(Guid userId, string? customerId, string priceId, string plan, string returnUrl);

    Task<string> Portal(string customerId);

    StripeEvent? Read(string body, string signature);

    Task<StripeEvent?> FetchCheckout(string sessionId);
}
