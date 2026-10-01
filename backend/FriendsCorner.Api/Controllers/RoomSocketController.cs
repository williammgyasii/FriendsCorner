using System.Net.WebSockets;
using System.Security.Claims;
using FriendsCorner.Api.Contracts;
using FriendsCorner.Core.Engines;
using FriendsCorner.Core.Managers;
using FriendsCorner.Infrastructure.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace FriendsCorner.Api.Controllers;

// Holds one browser's WebSocket open and turns each frame into one manager call.
[ApiController]
[Route("ws")]
public sealed class RoomSocketController : ControllerBase
{
    private readonly IRoomRegistryManager _registry;
    private readonly IClientMessageReader _reader;
    private readonly IWebSocketTextUtility _text;

    public RoomSocketController(IRoomRegistryManager registry, IClientMessageReader reader, IWebSocketTextUtility text)
    {
        _registry = registry;
        _reader = reader;
        _text = text;
    }

    [HttpGet("{roomId}")]
    public async Task<IActionResult> Connect(string roomId)
    {
        if (User.FindFirst(ClaimTypes.NameIdentifier)?.Value is not string raw || !Guid.TryParse(raw, out var userId))
        {
            return Unauthorized();
        }

        var gameName = User.FindFirst(AccountsController.GameNameClaim)?.Value
            ?? User.FindFirst(ClaimTypes.Name)?.Value
            ?? "Player";

        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            return BadRequest();
        }

        var room = await _registry.Find(roomId);
        if (room is null)
        {
            return NotFound();
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var cancellationToken = HttpContext.RequestAborted;
        if (await room.Join(socket, userId, gameName, cancellationToken) is not { } seat)
        {
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "room is full", CancellationToken.None);
            return new EmptyResult();
        }

        try
        {
            await Listen(room, seat, socket, cancellationToken);
        }
        finally
        {
            await room.Leave(seat, socket);
        }

        return new EmptyResult();
    }

    private async Task Listen(IRoomManager room, Seat seat, WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested &&
            await _text.Receive(socket, buffer, cancellationToken) is { } text)
        {
            switch (_reader.Read(text))
            {
                case Relay relay:
                    await room.Relay(seat, relay.Json, cancellationToken);
                    break;
                case Act act:
                    await room.Act(seat, act.Command, cancellationToken);
                    break;
            }
        }
    }
}
