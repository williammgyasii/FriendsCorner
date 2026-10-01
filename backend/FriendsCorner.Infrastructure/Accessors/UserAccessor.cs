using FriendsCorner.Core.Accessors;
using FriendsCorner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FriendsCorner.Infrastructure.Accessors;

public sealed class UserAccessor(IDbContextFactory<FriendsCornerDb> contexts) : IUserAccessor
{
    public async Task<AccountUser?> Find(string email)
    {
        await using var db = await contexts.CreateDbContextAsync();
        var row = await db.Users.SingleOrDefaultAsync(user => user.Email == email);
        return row is null ? null : new AccountUser(row.Id, row.Email, row.PasswordHash, row.Name, row.GameName);
    }

    public async Task Add(AccountUser user)
    {
        await using var db = await contexts.CreateDbContextAsync();
        db.Users.Add(new UserRow
        {
            Id = user.Id,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            Name = user.Name,
            GameName = user.GameName,
        });
        await db.SaveChangesAsync();
    }
}
