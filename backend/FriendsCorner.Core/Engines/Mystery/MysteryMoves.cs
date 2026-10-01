using FriendsCorner.Core.Engines.Games;

namespace FriendsCorner.Core.Engines.Mystery;

public sealed record OpenLead(string Lead) : GameMove;

public sealed record Accuse(string Suspect) : GameMove;

// Takes your pick back, which cancels a final answer.
public sealed record Withdraw : GameMove;
