using System.Security.Cryptography;
using System.Text;
using FriendsCorner.Infrastructure.Accessors;

namespace FriendsCorner.Tests;

public class StripeBillingAccessorTests
{
    private const string Secret = "whsec_test_secret";

    [Fact]
    public void A_signed_body_is_a_notice_and_a_bad_signature_is_not()
    {
        var user = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var body = $$"""
            {
              "id": "evt_test",
              "object": "event",
              "api_version": "2026-07-29.dahlia",
              "created": {{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}},
              "type": "checkout.session.completed",
              "data": {
                "object": {
                  "id": "cs_test",
                  "object": "checkout.session",
                  "customer": "cus_123",
                  "metadata": { "user_id": "{{user}}", "plan": "table" }
                }
              }
            }
            """;
        var accessor = new StripeBillingAccessor("sk_test", Secret);

        var notice = accessor.Read(body, Sign(body, Secret));
        var rejected = accessor.Read(body, Sign(body, "whsec_other"));

        Assert.NotNull(notice);
        Assert.Equal("cus_123", notice.Value.CustomerId);
        Assert.Equal(user, notice.Value.UserId);
        Assert.Equal("table", notice.Value.Plan);
        Assert.False(notice.Value.Ended);
        Assert.Null(rejected);
    }

    [Fact]
    public void Checkout_is_one_subscription_price_and_does_not_add_tax()
    {
        var draft = StripeBillingAccessor.Describe("price_corner");

        Assert.Equal("subscription", draft.Mode);
        Assert.Equal("price_corner", draft.PriceId);
        Assert.False(draft.ChargesTax);
        Assert.False(draft.ChoosesPaymentMethod);
        Assert.False(draft.ManagedPayments);
    }

    private static string Sign(string body, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = Encoding.UTF8.GetBytes($"{timestamp}.{body}");
        var mac = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), payload)).ToLowerInvariant();
        return $"t={timestamp},v1={mac}";
    }
}
