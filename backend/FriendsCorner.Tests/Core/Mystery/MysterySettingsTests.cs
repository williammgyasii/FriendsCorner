namespace FriendsCorner.Tests;

public class MysterySettingsTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 20, 0, 0, TimeSpan.Zero);
    private static readonly MysterySettings HardRace = new(MysteryLevel.Hard, MysteryMode.Race);

    private static LobbyEngine TwoIn()
    {
        var lobby = new LobbyEngine();
        lobby.Join(Seat.A);
        lobby.Join(Seat.B);
        Assert.True(lobby.TryPick(Seat.A, MysteryGame.GameId));
        return lobby;
    }

    [Fact]
    public void A_lobby_starts_easy_and_together()
    {
        Assert.Equal(new MysterySettings(MysteryLevel.Easy, MysteryMode.Together), new LobbyEngine().Mystery);
        Assert.Equal(MysterySettings.Default, new LobbyEngine().Mystery);
    }

    [Fact]
    public void The_game_master_sets_hard_race()
    {
        var lobby = TwoIn();

        Assert.True(lobby.TrySetMystery(Seat.A, HardRace));

        Assert.Equal(HardRace, lobby.Mystery);
    }

    [Fact]
    public void A_guest_cannot_change_the_settings()
    {
        var lobby = TwoIn();

        Assert.False(lobby.TrySetMystery(Seat.B, HardRace));

        Assert.Equal(MysterySettings.Default, lobby.Mystery);
    }

    [Fact]
    public void Settings_are_locked_during_the_countdown()
    {
        var lobby = TwoIn();
        lobby.TrySetReady(Seat.B, true);
        Assert.True(lobby.TryStart(Seat.A, Now));

        Assert.False(lobby.TrySetMystery(Seat.A, HardRace));

        Assert.Equal(MysterySettings.Default, lobby.Mystery);
    }

    [Fact]
    public void Changing_the_settings_clears_ready()
    {
        var lobby = TwoIn();
        lobby.TrySetReady(Seat.B, true);

        lobby.TrySetMystery(Seat.A, HardRace);

        Assert.False(lobby.IsReady(Seat.B));
    }

    [Fact]
    public void The_room_applies_a_settings_command_as_a_lobby_change()
    {
        var room = new RoomEngine();
        room.TryAddSeat(out _);
        room.TryAddSeat(out _);

        Assert.Equal(RoomChange.Lobby, room.Apply(Seat.A, new SetMysterySettings(HardRace), Now));
        Assert.Equal(RoomChange.None, room.Apply(Seat.B, new SetMysterySettings(MysterySettings.Default), Now));

        Assert.Equal(HardRace, room.Lobby.Mystery);
    }

    [Fact]
    public void A_mystery_starts_with_the_lobby_settings_and_a_rematch_keeps_them()
    {
        var room = new RoomEngine();
        room.TryAddSeat(out _);
        room.TryAddSeat(out _);
        room.Apply(Seat.A, new PickGame(MysteryGame.GameId), Now);
        room.Apply(Seat.A, new SetMysterySettings(HardRace), Now);
        room.Apply(Seat.B, new SetReady(true), Now);
        room.Apply(Seat.A, new StartGame(), Now);

        room.Advance(0.05, Now.AddSeconds(3));

        var game = Assert.IsType<MysteryGame>(room.Game);
        Assert.Equal(HardRace, game.State.Settings);
        Assert.True(game.TryFail(1));
        Assert.True(game.TryRematch());
        Assert.Equal(HardRace, game.State.Settings);
    }
}
