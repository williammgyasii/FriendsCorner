using FriendsCorner.Core.Accessors;
using FriendsCorner.Infrastructure.Accessors;
using Microsoft.EntityFrameworkCore;

namespace FriendsCorner.Tests;

public class UserAccessorTests
{
    [Fact]
    public async Task A_user_round_trips_by_email_and_a_second_insert_fails()
    {
        var contexts = await TestDatabase.Contexts();
        var users = new UserAccessor(contexts);
        var id = Guid.NewGuid();
        var email = $"{id:N}@x.com";

        await users.Add(new AccountUser(id, email, "hash", "Ada", "Countess"));
        var found = await users.Find(email);

        Assert.Equal(new AccountUser(id, email, "hash", "Ada", "Countess"), found);
        await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
            users.Add(new AccountUser(Guid.NewGuid(), email, "other", "Bea", "Countess")));
    }
}
