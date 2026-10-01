using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines.LetterTiles;
using FriendsCorner.Core.Engines.Mystery;

namespace FriendsCorner.Core.Engines.Games;

// Who will play, and what the lobby chose for them.
public sealed record GameStart(IReadOnlyList<Seat> Playing, MysterySettings Mystery);

// Which games a room can launch, and how each one starts. Without a word
// list the catalog has no Letter Tiles.
public sealed class GameCatalog
{
    private readonly Dictionary<string, Func<GameStart, IGameEngine?>> _starts = new()
    {
        [TicTacToeGame.GameId] = _ => new TicTacToeGame(Board.Empty()),
        [ChessGame.GameId] = _ => new ChessGame(ChessBoard.Start()),
        [MysteryGame.GameId] = start => MysteryGame.TryStart(start.Playing, start.Mystery),
    };

    public GameCatalog()
    {
    }

    public GameCatalog(IWordListAccessor words, Func<Random> random)
    {
        _starts[LetterTilesGame.GameId] = start =>
            start.Playing.Count < LetterTilesGame.MinPlayers ? null : LetterTilesGame.Start(start.Playing, words, random());
    }

    public bool TryStart(string id, IReadOnlyList<Seat> playing, out IGameEngine game) =>
        TryStart(id, new GameStart(playing, MysterySettings.Default), out game);

    public bool TryStart(string id, GameStart start, out IGameEngine game)
    {
        game = null!;
        if (!_starts.TryGetValue(id, out var begin) || begin(start) is not { } started)
        {
            return false;
        }

        game = started;
        return true;
    }
}
