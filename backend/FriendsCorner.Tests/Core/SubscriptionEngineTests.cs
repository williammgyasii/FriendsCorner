namespace FriendsCorner.Tests;

public class SubscriptionEngineTests
{
    [Fact]
    public void Only_corner_table_and_house_are_plans()
    {
        Assert.True(SubscriptionEngine.TryPlan("corner", out var corner));
        Assert.Equal(PlanCode.Corner, corner);
        Assert.True(SubscriptionEngine.TryPlan("table", out _));
        Assert.True(SubscriptionEngine.TryPlan("house", out _));
        Assert.False(SubscriptionEngine.TryPlan("weekly", out _));
        Assert.False(SubscriptionEngine.TryPlan(null, out _));
    }

    [Fact]
    public void A_newer_notice_sets_the_plan_and_an_older_one_does_not()
    {
        var current = new HostBilling(null, null, 0);

        var set = SubscriptionEngine.Apply(current, new BillingNotice(100, "cus_1", PlanCode.Table));
        var again = SubscriptionEngine.Apply(
            new HostBilling(set.Plan, set.CustomerId, set.At),
            new BillingNotice(100, "cus_1", PlanCode.Table));
        var cleared = SubscriptionEngine.Apply(
            new HostBilling(PlanCode.Table, "cus_1", 100),
            new BillingNotice(200, "cus_other", null));
        var older = SubscriptionEngine.Apply(
            new HostBilling(PlanCode.Corner, "cus_1", 300),
            new BillingNotice(200, "cus_1", PlanCode.Table));

        Assert.True(set.Applied);
        Assert.Equal(PlanCode.Table, set.Plan);
        Assert.Equal("cus_1", set.CustomerId);
        Assert.False(again.Applied);
        Assert.Equal("cus_1", again.CustomerId);
        Assert.Null(cleared.Plan);
        Assert.Equal("cus_1", cleared.CustomerId);
        Assert.False(older.Applied);
        Assert.Equal(PlanCode.Corner, older.Plan);
    }
}
