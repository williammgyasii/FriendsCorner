using FriendsCorner.Core.Accessors;
using FriendsCorner.Infrastructure.Accessors;
using Microsoft.EntityFrameworkCore;

namespace FriendsCorner.Tests;

public class HostPlanAccessorTests
{
    [Fact]
    public async Task A_plan_round_trips_and_a_customer_id_is_unique()
    {
        var contexts = await TestDatabase.Contexts();
        var users = new UserAccessor(contexts);
        var plans = new HostPlanAccessor(contexts);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        await users.Add(new AccountUser(first, $"{first:N}@x.com", "hash", "Ada", "Countess"));
        await users.Add(new AccountUser(second, $"{second:N}@x.com", "hash", "Bea", "Countess"));

        var customer = $"cus_{first:N}";
        await plans.Save(new HostPlanRow(first, customer, "table", 100));
        var found = await plans.Find(first);
        var byCustomer = await plans.FindByCustomer(customer);

        Assert.Equal(new HostPlanRow(first, customer, "table", 100), found);
        Assert.Equal(found, byCustomer);
        await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
            plans.Save(new HostPlanRow(second, customer, "corner", 200)));
    }
}
