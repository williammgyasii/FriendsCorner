using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Managers;

namespace FriendsCorner.Tests;

public class AccountManagerTests
{
    [Fact]
    public async Task Registering_stores_one_user_and_signing_in_returns_that_guid()
    {
        var users = new FakeUsers();
        var passwords = new FakePasswords();
        var accounts = new AccountManager(users, passwords);

        var created = await accounts.Register("Ada Lovelace", "Countess", "ada@x.com", "correct-horse");
        var again = await accounts.Register("Ada", "Other", "Ada@x.com", "another-password");
        var shared = await accounts.Register("Bea", "Countess", "bea@x.com", "correct-horse");
        var signedIn = await accounts.SignIn(" Ada@x.com ", "correct-horse");

        Assert.Equal(AccountStatus.Created, created.Status);
        Assert.Equal(AccountStatus.Duplicate, again.Status);
        Assert.Equal(AccountStatus.Created, shared.Status);
        Assert.Equal(2, users.Rows.Count);
        Assert.Equal("Ada Lovelace", users.Rows[0].Name);
        Assert.Equal("Countess", users.Rows[0].GameName);
        Assert.Equal("Countess", users.Rows[1].GameName);
        Assert.Equal(created.Id, signedIn.Id);
        Assert.Equal("Ada Lovelace", signedIn.Name);
        Assert.Equal("Countess", signedIn.GameName);
        Assert.Equal(AccountStatus.SignedIn, signedIn.Status);
    }

    [Fact]
    public async Task A_short_password_creates_nothing()
    {
        var users = new FakeUsers();
        var accounts = new AccountManager(users, new FakePasswords());

        var result = await accounts.Register("Ada", "Countess", "ada@x.com", "short");

        Assert.Equal(AccountStatus.TooShort, result.Status);
        Assert.Empty(users.Rows);
    }

    [Fact]
    public async Task A_blank_game_name_creates_nothing()
    {
        var users = new FakeUsers();
        var accounts = new AccountManager(users, new FakePasswords());

        var result = await accounts.Register("Ada", "   ", "ada@x.com", "correct-horse");

        Assert.Equal(AccountStatus.InvalidGameName, result.Status);
        Assert.Empty(users.Rows);
    }

    private sealed class FakeUsers : IUserAccessor
    {
        public List<AccountUser> Rows { get; } = [];

        public Task<AccountUser?> Find(string email) =>
            Task.FromResult(Rows.SingleOrDefault(user => user.Email == email));

        public Task Add(AccountUser user)
        {
            Rows.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswords : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";

        public bool Matches(string password, string hash) => hash == Hash(password);
    }
}
