namespace FriendsCorner.Core.Engines;

// What a billing event means. Price ids, HTTP, and Stripe stay outside.
public static class SubscriptionEngine
{
    public static bool TryPlan(string? plan, out PlanCode code)
    {
        switch (plan)
        {
            case "corner":
                code = PlanCode.Corner;
                return true;
            case "table":
                code = PlanCode.Table;
                return true;
            case "house":
                code = PlanCode.House;
                return true;
            default:
                code = default;
                return false;
        }
    }

    public static PlanUpdate Apply(HostBilling current, BillingNotice notice)
    {
        if (notice.At <= current.At)
        {
            return new PlanUpdate(current.Plan, current.CustomerId, current.At, false);
        }

        var customer = notice.Plan is null ? current.CustomerId : notice.CustomerId ?? current.CustomerId;
        return new PlanUpdate(notice.Plan, customer, notice.At, true);
    }
}

public enum PlanCode
{
    Corner,
    Table,
    House,
}

public readonly record struct HostBilling(PlanCode? Plan, string? CustomerId, long At);

public readonly record struct BillingNotice(long At, string? CustomerId, PlanCode? Plan);

public readonly record struct PlanUpdate(PlanCode? Plan, string? CustomerId, long At, bool Applied);
