using System.Security.Claims;
using FriendsCorner.Api;
using FriendsCorner.Infrastructure;
using FriendsCorner.Api.Controllers;
using FriendsCorner.Core.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FriendsCorner.Tests;

public class RoomControllerTests : IDisposable
{
    private const string UnusedDatabase = "postgresql://user:pass@localhost/friends";

    private readonly ServiceProvider _services = new ServiceCollection().AddInfrastructure(UnusedDatabase).AddApi("test-ticket-key").BuildServiceProvider();

    private IRoomRegistryManager Registry => _services.GetRequiredService<IRoomRegistryManager>();

    public void Dispose() => _services.Dispose();

    [Fact]
    public void Creating_a_room_returns_the_id_of_a_live_room()
    {
        var created = Host().Create().Value;

        Assert.NotNull(created);
        Assert.True(Registry.IsLive(created.Id));
    }

    [Fact]
    public void Opening_a_lobby_without_a_host_is_refused()
    {
        var registry = new CountingRegistry();
        var controller = new RoomsController(registry);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        Assert.IsType<UnauthorizedResult>(controller.Create().Result);
        Assert.Equal(0, registry.Created);
    }

    private RoomsController Host()
    {
        var controller = new RoomsController(Registry);
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            "Cookies");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
        return controller;
    }

    private sealed class CountingRegistry : IRoomRegistryManager
    {
        public int Created { get; private set; }

        public string Create(Guid hostUserId)
        {
            Created++;
            return "room";
        }

        public bool IsLive(string id) => false;

        public Task<IRoomManager?> Find(string id) => Task.FromResult<IRoomManager?>(null);
    }

    [Fact]
    public void A_live_room_answers_no_content()
    {
        var id = Registry.Create(Guid.NewGuid());

        Assert.IsType<NoContentResult>(new RoomsController(Registry).Check(id));
    }

    [Fact]
    public void An_unknown_room_answers_not_found()
    {
        Assert.IsType<NotFoundResult>(new RoomsController(Registry).Check("nope"));
    }

    [Fact]
    public async Task A_socket_with_no_cookie_is_refused()
    {
        var controller = ActivatorUtilities.CreateInstance<RoomSocketController>(_services);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        Assert.IsType<UnauthorizedResult>(await controller.Connect("any-room"));
    }

    [Fact]
    public async Task A_signed_in_request_that_is_not_a_socket_is_a_bad_request()
    {
        var controller = ActivatorUtilities.CreateInstance<RoomSocketController>(_services);
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Cookies");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };

        Assert.IsType<BadRequestResult>(await controller.Connect("any-room"));
    }
}
