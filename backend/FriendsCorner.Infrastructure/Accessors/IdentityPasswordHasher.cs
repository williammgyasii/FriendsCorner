using FriendsCorner.Core.Accessors;
using Microsoft.AspNetCore.Identity;

namespace FriendsCorner.Infrastructure.Accessors;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<AccountUser> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Matches(string password, string hash) =>
        _hasher.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}
