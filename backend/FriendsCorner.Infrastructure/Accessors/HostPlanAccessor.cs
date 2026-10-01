using FriendsCorner.Core.Accessors;
using FriendsCorner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FriendsCorner.Infrastructure.Accessors;

public sealed class HostPlanAccessor(IDbContextFactory<FriendsCornerDb> contexts) : IHostPlanAccessor
{
    public async Task<HostPlanRow?> Find(Guid userId)
    {
        await using var db = await contexts.CreateDbContextAsync();
        var row = await db.Users.SingleOrDefaultAsync(user => user.Id == userId);
        return row is null ? null : ToPlan(row);
    }

    public async Task<HostPlanRow?> FindByCustomer(string customerId)
    {
        await using var db = await contexts.CreateDbContextAsync();
        var row = await db.Users.SingleOrDefaultAsync(user => user.StripeCustomerId == customerId);
        return row is null ? null : ToPlan(row);
    }

    public async Task Save(HostPlanRow plan)
    {
        await using var db = await contexts.CreateDbContextAsync();
        var row = await db.Users.SingleAsync(user => user.Id == plan.UserId);
        row.StripeCustomerId = plan.CustomerId;
        row.Plan = plan.Plan;
        row.BillingEventAt = plan.EventAt;
        await db.SaveChangesAsync();
    }

    private static HostPlanRow ToPlan(UserRow row) =>
        new(row.Id, row.StripeCustomerId, row.Plan, row.BillingEventAt);
}
