using System.Security.Claims;
using FriendsCorner.Api.Contracts;
using FriendsCorner.Core.Engines;
using FriendsCorner.Core.Managers;
using Microsoft.AspNetCore.Mvc;

namespace FriendsCorner.Api.Controllers;

[ApiController]
[Route("rooms")]
public sealed class RoomsController : ControllerBase
{
    private readonly IRoomRegistryManager _registry;

    public RoomsController(IRoomRegistryManager registry) => _registry = registry;

    [HttpPost]
    public ActionResult<RoomCreated> Create()
    {
        if (User.FindFirst(ClaimTypes.NameIdentifier)?.Value is not string raw || !Guid.TryParse(raw, out var host))
        {
            return Unauthorized();
        }

        return new RoomCreated(_registry.Create(host));
    }

    [HttpGet("{roomId}")]
    public IActionResult Check(string roomId) => _registry.IsLive(roomId) ? NoContent() : NotFound();
}
