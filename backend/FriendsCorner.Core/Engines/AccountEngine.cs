namespace FriendsCorner.Core.Engines;

// The account rules. Hashing, storage, and the cookie live elsewhere.
public static class AccountEngine
{
    public const int MinimumPasswordLength = 8;
    public const int MaximumNameLength = 40;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    // A label people read. Empty and over-long are the same refusal.
    public static string? AcceptName(string name)
    {
        var trimmed = name.Trim();
        return trimmed.Length is >= 1 and <= MaximumNameLength ? trimmed : null;
    }

    public static bool AcceptsPassword(string password) => password.Length >= MinimumPasswordLength;

    // An unknown email and a wrong password are the same decision, so a
    // caller cannot tell which one failed.
    public static SignInDecision SignIn(Guid? userId, bool passwordMatches) =>
        userId is Guid id && passwordMatches ? new SignInDecision(id) : new SignInDecision(null);
}

public readonly record struct SignInDecision(Guid? Id);
