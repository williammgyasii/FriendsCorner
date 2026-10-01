using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FriendsCorner.Api.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FriendsCorner.Api;

// Signs the session cookie with the configured key, so a restart can still read it.
public sealed class ConfiguredTicketFormat(string key) : ISecureDataFormat<AuthenticationTicket>
{
    private readonly byte[] _key = SHA256.HashData(Encoding.UTF8.GetBytes(key));

    public string Protect(AuthenticationTicket data, string? purpose) => Protect(data);

    public AuthenticationTicket? Unprotect(string? protectedText, string? purpose) => Unprotect(protectedText);

    public string Protect(AuthenticationTicket data)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new TicketBody(
            data.Principal.FindFirst(ClaimTypes.NameIdentifier)!.Value,
            data.Principal.FindFirst(ClaimTypes.Email)!.Value,
            data.Properties.ExpiresUtc?.ToUnixTimeSeconds(),
            data.Principal.FindFirst(ClaimTypes.Name)?.Value,
            data.Principal.FindFirst(AccountsController.GameNameClaim)?.Value));
        var mac = HMACSHA256.HashData(_key, payload);
        return $"{Encode(payload)}.{Encode(mac)}";
    }

    public AuthenticationTicket? Unprotect(string? protectedText)
    {
        if (string.IsNullOrEmpty(protectedText))
        {
            return null;
        }

        var parts = protectedText.Split('.');
        if (parts.Length != 2)
        {
            return null;
        }

        byte[] payload;
        byte[] mac;
        try
        {
            payload = Decode(parts[0]);
            mac = Decode(parts[1]);
        }
        catch (FormatException)
        {
            return null;
        }

        if (!CryptographicOperations.FixedTimeEquals(mac, HMACSHA256.HashData(_key, payload)))
        {
            return null;
        }

        var body = JsonSerializer.Deserialize<TicketBody>(payload);
        if (body is null || body.Exp is long exp && DateTimeOffset.FromUnixTimeSeconds(exp) <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, body.Id),
            new(ClaimTypes.Email, body.Email),
        };
        if (body.Name is not null)
        {
            claims.Add(new Claim(ClaimTypes.Name, body.Name));
        }

        if (body.GameName is not null)
        {
            claims.Add(new Claim(AccountsController.GameNameClaim, body.GameName));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties();
        if (body.Exp is long expires)
        {
            properties.ExpiresUtc = DateTimeOffset.FromUnixTimeSeconds(expires);
        }

        return new AuthenticationTicket(new ClaimsPrincipal(identity), properties, CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static string Encode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Decode(string text)
    {
        var padded = text.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }

    // Name and game name are absent on cookies issued before those claims existed.
    private sealed record TicketBody(string Id, string Email, long? Exp, string? Name = null, string? GameName = null);
}
