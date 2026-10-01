using FriendsCorner.Api.Contracts;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

namespace FriendsCorner.Api;

// Our own services at the web edge. MVC itself is added by the host in Program.
public static class ApiRegistration
{
    public static IServiceCollection AddApi(this IServiceCollection services, string? ticketKey)
    {
        if (string.IsNullOrWhiteSpace(ticketKey))
        {
            throw new InvalidOperationException("Auth:TicketKey is missing.");
        }

        // The host normally supplies these. Tests build this container on its own.
        services.AddLogging();
        services.AddWebEncoders();
        services.AddDataProtection();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "friends.session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.TicketDataFormat = new ConfiguredTicketFormat(ticketKey);
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
            });
        services.AddSingleton<IClientMessageReader, ClientMessageReader>();
        return services;
    }
}
