using FriendsCorner.Api;
using FriendsCorner.Api.Contracts;
using FriendsCorner.Api.Controllers;
using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Managers;
using FriendsCorner.Core.Utilities;
using FriendsCorner.Infrastructure;
using FriendsCorner.Infrastructure.Persistence;
using FriendsCorner.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FriendsCorner.Tests;

// The container is the composition root: if a registration is missing or has
// the wrong lifetime, these fail here instead of on the first request.
public class InfrastructureRegistrationTests
{
    private const string UnusedDatabase = "postgresql://user:pass@localhost/friends";

    private static ServiceProvider Build() =>
        new ServiceCollection()
            .AddInfrastructure(UnusedDatabase).AddApi()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    [Fact]
    public void Both_controllers_can_be_built_from_the_container()
    {
        using var provider = Build();

        Assert.NotNull(ActivatorUtilities.CreateInstance<RoomsController>(provider));
        Assert.NotNull(ActivatorUtilities.CreateInstance<RoomSocketController>(provider));
    }

    [Fact]
    public void The_whole_app_shares_one_room_registry()
    {
        using var provider = Build();

        Assert.Same(
            provider.GetRequiredService<IRoomRegistryManager>(),
            provider.GetRequiredService<IRoomRegistryManager>());
    }

    [Fact]
    public void Each_room_gets_its_own_manager()
    {
        using var provider = Build();
        var factory = provider.GetRequiredService<IRoomManagerFactory>();

        Assert.NotSame(factory.Create("room-1", () => { }), factory.Create("room-2", () => { }));
    }

    [Fact]
    public void The_helpers_resolve_too()
    {
        using var provider = Build();

        Assert.NotNull(provider.GetRequiredService<IClientMessageReader>());
        Assert.NotNull(provider.GetRequiredService<IWebSocketTextUtility>());
        Assert.NotNull(provider.GetRequiredService<ITickerUtility>());
    }

    [Fact]
    public void The_word_list_is_loaded_once_for_the_whole_app()
    {
        using var provider = Build();

        Assert.Same(provider.GetRequiredService<IWordListAccessor>(), provider.GetRequiredService<IWordListAccessor>());
    }

    [Fact]
    public void A_room_from_the_container_can_start_letter_tiles()
    {
        using var provider = Build();
        var room = provider.GetRequiredService<RoomEngine>();
        Assert.True(room.TryAddSeat(out _));
        Assert.True(room.TryAddSeat(out _));

        Assert.True(room.TryLaunch(LetterTilesGame.GameId));
        Assert.IsType<LetterTilesGame>(room.Game);
    }

    [Fact]
    public void The_singleton_tables_get_a_context_factory_not_a_captured_context()
    {
        using var provider = Build();

        Assert.Same(
            provider.GetRequiredService<IDbContextFactory<FriendsCornerDb>>(),
            provider.GetRequiredService<IDbContextFactory<FriendsCornerDb>>());
        Assert.NotNull(provider.GetRequiredService<IBoardTableAccessor>());
        Assert.NotNull(provider.GetRequiredService<IChessTableAccessor>());
    }
}
