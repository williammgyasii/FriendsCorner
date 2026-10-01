namespace FriendsCorner.Core.Accessors;

public sealed record AccountUser(Guid Id, string Email, string PasswordHash, string Name, string GameName);

public interface IUserAccessor
{
    Task<AccountUser?> Find(string email);

    Task Add(AccountUser user);
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Matches(string password, string hash);
}
