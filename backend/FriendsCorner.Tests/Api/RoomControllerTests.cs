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

    private readonly ServiceProvider _services = new ServiceCollection().AddInfrastructure(UnusedDatabase).AddApi().BuildServiceProvider();

    private IRoomRegistryManager Registry => _services.GetRequiredService<IRoomRegistryManager>();

    public void Dispose() => _services.Dispose();

    [Fact]
    public void Creating_a_room_returns_the_id_of_a_live_room()
    {
        var created = new RoomsController(Registry).Create().Value;

        Assert.NotNull(created);
        Assert.True(Registry.IsLive(created.Id));
    }

    [Fact]
    public void A_live_room_answers_no_content()
    {
        var id = Registry.Create();

        Assert.IsType<NoContentResult>(new RoomsController(Registry).Check(id));
    }

    [Fact]
    public void An_unknown_room_answers_not_found()
    {
        Assert.IsType<NotFoundResult>(new RoomsController(Registry).Check("nope"));
    }

    [Fact]
    public async Task A_plain_request_to_the_socket_route_is_a_bad_request()
    {
        var controller = ActivatorUtilities.CreateInstance<RoomSocketController>(_services);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        Assert.IsType<BadRequestResult>(await controller.Connect("any-room"));
    }
}
