using System.Security.Claims;
using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Managers;
using Microsoft.AspNetCore.Mvc;

namespace FriendsCorner.Api.Controllers;

public sealed record BillingRequest(string Action, string? Plan = null, string? ReturnUrl = null, string? SessionId = null);

public sealed record BillingLink(string Url);

[ApiController]
[Route("billing")]
public sealed class BillingController(SubscriptionManager billing, IStripeAccessor stripe) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post(BillingRequest request)
    {
        if (User.FindFirst(ClaimTypes.NameIdentifier)?.Value is not string raw || !Guid.TryParse(raw, out var userId))
        {
            return Unauthorized();
        }

        var result = request.Action switch
        {
            "checkout" => await billing.Checkout(userId, request.Plan, request.ReturnUrl),
            "portal" => await billing.Portal(userId),
            "confirm" => await billing.ConfirmCheckout(userId, request.SessionId),
            _ => new BillingResult(BillingStatus.UnknownPlan, null),
        };
        return result.Status switch
        {
            BillingStatus.Checkout or BillingStatus.Portal => Ok(new BillingLink(result.Url!)),
            BillingStatus.Confirmed => NoContent(),
            BillingStatus.UnknownPlan => BadRequest(),
            BillingStatus.AlreadySubscribed => Conflict(),
            BillingStatus.NoCustomer => NotFound(),
            BillingStatus.SessionNotFound => NotFound(),
            BillingStatus.SessionMismatch => Forbid(),
            BillingStatus.NotConfigured => StatusCode(StatusCodes.Status503ServiceUnavailable),
            _ => BadRequest(),
        };
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var notice = stripe.Read(await reader.ReadToEndAsync(), Request.Headers["Stripe-Signature"].ToString());
        if (notice is null)
        {
            return BadRequest();
        }

        await billing.Apply(notice.Value);
        return Ok();
    }
}
