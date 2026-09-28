using FriendsCorner.Core.Engines;

namespace FriendsCorner.Api.Contracts;

public abstract record ClientMessage;

// A face-call signal, passed to the call partner untouched.
public sealed record Relay(string Json) : ClientMessage;

public sealed record Act(RoomCommand Command) : ClientMessage;
