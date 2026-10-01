using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;

namespace FriendsCorner.Core.Managers;

public enum AccountStatus
{
    Created,
    SignedIn,
    Duplicate,
    TooShort,
    InvalidName,
    InvalidGameName,
    Rejected,
}

public readonly record struct AccountResult(AccountStatus Status, Guid? Id, string? Name = null, string? GameName = null);

// Register, then sign in. The engine decides; this only puts the steps in order.
public sealed class AccountManager(IUserAccessor users, IPasswordHasher passwords)
{
    public async Task<AccountResult> Register(string name, string gameName, string email, string password)
    {
        if (AccountEngine.AcceptName(name) is not string acceptedName)
        {
            return new AccountResult(AccountStatus.InvalidName, null);
        }

        if (AccountEngine.AcceptName(gameName) is not string acceptedGameName)
        {
            return new AccountResult(AccountStatus.InvalidGameName, null);
        }

        if (!AccountEngine.AcceptsPassword(password))
        {
            return new AccountResult(AccountStatus.TooShort, null);
        }

        var normalized = AccountEngine.NormalizeEmail(email);
        if (await users.Find(normalized) is not null)
        {
            return new AccountResult(AccountStatus.Duplicate, null);
        }

        var user = new AccountUser(Guid.NewGuid(), normalized, passwords.Hash(password), acceptedName, acceptedGameName);
        await users.Add(user);
        return new AccountResult(AccountStatus.Created, user.Id, acceptedName, acceptedGameName);
    }

    public async Task<AccountResult> SignIn(string email, string password)
    {
        var normalized = AccountEngine.NormalizeEmail(email);
        var user = await users.Find(normalized);
        var matches = user is not null && passwords.Matches(password, user.PasswordHash);
        var decision = AccountEngine.SignIn(user?.Id, matches);
        return decision.Id is Guid id
            ? new AccountResult(AccountStatus.SignedIn, id, user!.Name, user.GameName)
            : new AccountResult(AccountStatus.Rejected, null);
    }
}
