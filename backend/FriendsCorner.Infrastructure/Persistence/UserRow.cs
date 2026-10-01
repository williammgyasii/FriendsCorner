namespace FriendsCorner.Infrastructure.Persistence;

public sealed class UserRow
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string Name { get; set; }
    public required string GameName { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? Plan { get; set; }
    public long BillingEventAt { get; set; }
}
