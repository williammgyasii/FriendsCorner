namespace FriendsCorner.Tests;

public class LetterTilesGameTests
{
    private static readonly WordsAndScoreTests.FakeWords Words = new("CAT", "CATS", "AT");

    private static Tile[] Rack(string letters) =>
        letters.Select(letter => letter == Tile.Unassigned ? Tile.Blank : new Tile(letter)).ToArray();

    private static PlacedTile[] Across(int start, string letters) =>
        letters.Select((letter, i) => new PlacedTile(start + i, new Tile(letter))).ToArray();

    private static LetterTilesGame Start(params Seat[] playing) =>
        LetterTilesGame.Start(playing, Words, new Random(7));

    // A two-player game where A holds CATSEEN and the bag holds the given count.
    private static LetterTilesGame Staged(int bag, string aRack = "CATSEEN", string bRack = "BDFGHIO")
    {
        var game = Start(Seat.A, Seat.B);
        var state = game.State with
        {
            Bag = game.State.Bag.Take(bag).ToArray(),
            Racks = new Dictionary<Seat, IReadOnlyList<Tile>> { [Seat.A] = Rack(aRack), [Seat.B] = Rack(bRack) },
        };
        return new LetterTilesGame(state, Words, new Random(7));
    }

    private static int RackValue(LetterTilesGame game, Seat seat) => game.State.Racks[seat].Sum(TileSet.ValueOf);

    [Fact]
    public void Three_players_start_with_seven_tiles_each_and_A_to_move()
    {
        var game = Start(Seat.A, Seat.B, Seat.C);

        Assert.All(game.State.Racks.Values, rack => Assert.Equal(7, rack.Count));
        Assert.Equal(79, game.State.Bag.Count);
        Assert.Equal(Seat.A, game.State.ToMove);
        Assert.Equal(LetterTilesGame.GameId, game.Id);
    }

    [Fact]
    public void The_same_randomness_deals_the_same_racks()
    {
        var first = Start(Seat.A, Seat.B);
        var second = Start(Seat.A, Seat.B);

        Assert.Equal(first.State.Racks[Seat.A], second.State.Racks[Seat.A]);
        Assert.Equal(first.State.Racks[Seat.B], second.State.Racks[Seat.B]);
        Assert.Equal(first.State.Bag, second.State.Bag);
    }

    [Fact]
    public void A_seat_out_of_turn_is_refused_and_nothing_changes()
    {
        var game = Staged(bag: 50);
        var before = game.State;

        Assert.False(game.TryPlay(Seat.B, new PassTurn()));

        Assert.Same(before, game.State);
        Assert.Equal(RefusalReason.NotYourTurn, game.RefusalFor(Seat.B)!.Reason);
    }

    [Fact]
    public void A_play_scores_refills_the_rack_and_passes_the_turn()
    {
        var game = Staged(bag: 50);

        Assert.True(game.TryPlay(Seat.A, new PlayTiles(Across(111, "CAT"))));

        Assert.Equal(7, game.State.Racks[Seat.A].Count);
        Assert.Equal(47, game.State.Bag.Count);
        Assert.Equal(10, game.State.Scores[Seat.A]);
        Assert.Equal(Seat.B, game.State.ToMove);
        Assert.Equal(new Tile('A'), game.State.Board[112]);
        var last = game.State.LastPlay!;
        Assert.Equal((Seat.A, 10), (last.Seat, last.Score));
        Assert.Equal(["CAT"], last.Words);
    }

    [Fact]
    public void An_unknown_word_is_refused_for_that_seat_only_until_its_next_accepted_move()
    {
        var game = Staged(bag: 50, aRack: "QXZSEEN");

        Assert.False(game.TryPlay(Seat.A, new PlayTiles(Across(111, "QXZ"))));

        var refusal = game.RefusalFor(Seat.A)!;
        Assert.Equal(RefusalReason.NotAWord, refusal.Reason);
        Assert.Equal(["QXZ"], refusal.Words);
        Assert.Null(game.RefusalFor(Seat.B));
        Assert.Equal(Seat.A, game.State.ToMove);
        Assert.All(game.State.Board, Assert.Null);

        Assert.True(game.TryPlay(Seat.A, new PassTurn()));
        Assert.Null(game.RefusalFor(Seat.A));
    }

    [Fact]
    public void A_placement_refusal_keeps_the_turn()
    {
        var game = Staged(bag: 50);

        Assert.False(game.TryPlay(Seat.A, new PlayTiles(Across(0, "CAT"))));

        Assert.Equal(RefusalReason.FirstMustCoverCentre, game.RefusalFor(Seat.A)!.Reason);
        Assert.Equal(Seat.A, game.State.ToMove);
    }

    [Fact]
    public void Exchanging_with_a_full_bag_keeps_counts_and_passes_the_turn()
    {
        var game = Staged(bag: 20);

        Assert.True(game.TryPlay(Seat.A, new ExchangeTiles(Rack("CAT"))));

        Assert.Equal(7, game.State.Racks[Seat.A].Count);
        Assert.Equal(20, game.State.Bag.Count);
        Assert.Equal(0, game.State.Scores[Seat.A]);
        Assert.Equal(Seat.B, game.State.ToMove);
    }

    [Fact]
    public void Exchanging_with_a_small_bag_is_refused()
    {
        var game = Staged(bag: 6);

        Assert.False(game.TryPlay(Seat.A, new ExchangeTiles(Rack("CAT"))));

        Assert.Equal(RefusalReason.BagTooSmall, game.RefusalFor(Seat.A)!.Reason);
        Assert.Equal(Seat.A, game.State.ToMove);
    }

    [Fact]
    public void Exchanging_a_tile_not_on_the_rack_is_refused()
    {
        var game = Staged(bag: 20);

        Assert.False(game.TryPlay(Seat.A, new ExchangeTiles(Rack("Q"))));

        Assert.Equal(RefusalReason.NotInRack, game.RefusalFor(Seat.A)!.Reason);
    }

    [Fact]
    public void Passing_scores_nothing_and_passes_the_turn()
    {
        var game = Staged(bag: 50);

        Assert.True(game.TryPlay(Seat.A, new PassTurn()));

        Assert.Equal(0, game.State.Scores[Seat.A]);
        Assert.Equal(Seat.B, game.State.ToMove);
        Assert.Equal(1, game.State.ScorelessTurns);
    }

    [Fact]
    public void Six_scoreless_turns_end_the_game_and_each_loses_their_rack()
    {
        var game = Staged(bag: 50);
        var (aRack, bRack) = (RackValue(game, Seat.A), RackValue(game, Seat.B));

        for (var turn = 0; turn < 6; turn++)
        {
            Assert.True(game.TryPlay(game.State.ToMove, new PassTurn()));
        }

        Assert.NotNull(game.State.Outcome);
        Assert.Equal(-aRack, game.State.Scores[Seat.A]);
        Assert.Equal(-bRack, game.State.Scores[Seat.B]);
        Assert.False(game.TryPlay(game.State.ToMove, new PassTurn()));
        Assert.Equal(RefusalReason.GameOver, game.RefusalFor(game.State.ToMove)!.Reason);
    }

    [Fact]
    public void A_refused_command_is_not_a_scoreless_turn()
    {
        var game = Staged(bag: 50);

        Assert.False(game.TryPlay(Seat.B, new PassTurn()));

        Assert.Equal(0, game.State.ScorelessTurns);
    }

    [Fact]
    public void Going_out_takes_the_other_racks_value()
    {
        var game = Staged(bag: 0, aRack: "S", bRack: "QE");
        var board = game.State.Board.ToArray();
        foreach (var tile in Across(111, "CAT"))
        {
            board[tile.Square] = tile.Tile;
        }

        game = new LetterTilesGame(game.State with { Board = board }, Words, new Random(7));

        Assert.True(game.TryPlay(Seat.A, new PlayTiles([new PlacedTile(114, new Tile('S'))])));

        Assert.NotNull(game.State.Outcome);
        Assert.Equal(6 + 11, game.State.Scores[Seat.A]);
        Assert.Equal(-11, game.State.Scores[Seat.B]);
        Assert.Equal([Seat.A], game.State.Outcome.Winners);
    }

    [Fact]
    public void A_tie_has_several_winners()
    {
        var game = Start(Seat.A, Seat.B, Seat.C);
        var empty = Array.Empty<Tile>();
        game = new LetterTilesGame(
            game.State with
            {
                Racks = new Dictionary<Seat, IReadOnlyList<Tile>> { [Seat.A] = empty, [Seat.B] = empty, [Seat.C] = empty },
                Scores = new Dictionary<Seat, int> { [Seat.A] = 210, [Seat.B] = 210, [Seat.C] = 150 },
                ScorelessTurns = 5,
            },
            Words,
            new Random(7));

        Assert.True(game.TryPlay(Seat.A, new PassTurn()));

        Assert.Equal([Seat.A, Seat.B], game.State.Outcome!.Winners);
    }

    [Fact]
    public void A_rematch_waits_for_the_end_then_the_next_seat_starts()
    {
        var game = Staged(bag: 50);
        Assert.False(game.TryRematch());

        for (var turn = 0; turn < 6; turn++)
        {
            Assert.True(game.TryPlay(game.State.ToMove, new PassTurn()));
        }

        Assert.True(game.TryRematch());

        Assert.Null(game.State.Outcome);
        Assert.Equal(Seat.B, game.State.ToMove);
        Assert.Equal(Seat.B, game.State.Starter);
        Assert.Equal(86, game.State.Bag.Count);
        Assert.All(game.State.Scores.Values, score => Assert.Equal(0, score));
        Assert.All(game.State.Board, Assert.Null);
    }

    [Fact]
    public void Tiles_needs_at_least_two_players()
    {
        var catalog = new GameCatalog(Words, () => new Random(7));

        Assert.False(catalog.TryStart(LetterTilesGame.GameId, [Seat.A], out _));
        Assert.True(catalog.TryStart(LetterTilesGame.GameId, [Seat.A, Seat.B], out var game));
        Assert.IsType<LetterTilesGame>(game);
    }

    [Fact]
    public void A_waiting_seat_can_preview_a_word_and_nothing_changes()
    {
        var game = Staged(bag: 50, bRack: "CATBDFG");
        var before = game.State;

        Assert.True(before.TryPreview(Seat.B, Across(111, "CAT"), Words, out var judgement, out _));

        Assert.Equal(["CAT"], judgement.Words);
        Assert.Equal(10, judgement.Score);
        Assert.Same(before, game.State);
        Assert.Equal(Seat.A, game.State.ToMove);
    }

    [Fact]
    public void Previewing_an_unknown_word_answers_the_refusal_and_stores_none()
    {
        var game = Staged(bag: 50, aRack: "QXZEEEN");

        Assert.False(game.State.TryPreview(Seat.A, Across(111, "QXZ"), Words, out _, out var refusal));

        Assert.Equal(RefusalReason.NotAWord, refusal!.Reason);
        Assert.Equal(["QXZ"], refusal.Words);
        Assert.Null(game.RefusalFor(Seat.A));
    }

    [Fact]
    public void Previewing_a_broken_line_answers_gap()
    {
        var game = Staged(bag: 50);
        PlacedTile[] split = [new(111, new Tile('C')), new(113, new Tile('T'))];

        Assert.False(game.State.TryPreview(Seat.A, split, Words, out _, out var refusal));

        Assert.Equal(RefusalReason.Gap, refusal!.Reason);
    }

    [Fact]
    public void Previewing_after_the_end_answers_game_over()
    {
        var game = Staged(bag: 50);
        var ended = game.State with { Outcome = new TilesOutcome([Seat.A]) };

        Assert.False(ended.TryPreview(Seat.A, Across(111, "CAT"), Words, out _, out var refusal));

        Assert.Equal(RefusalReason.GameOver, refusal!.Reason);
    }

    [Fact]
    public void A_preview_matches_the_play_it_describes()
    {
        var game = Staged(bag: 50, bRack: "SBDFGHI");
        game.TryPlay(Seat.A, new PlayTiles(Across(111, "CAT")));
        PlacedTile[] hook = [new(114, new Tile('S'))];

        Assert.True(game.State.TryPreview(Seat.B, hook, Words, out var judgement, out _));
        Assert.True(game.TryPlay(Seat.B, new PlayTiles(hook)));

        var last = game.State.LastPlay!;
        Assert.Equal(judgement.Words, last.Words);
        Assert.Equal(judgement.Score, last.Score);
        Assert.Equal(6, judgement.Score);
    }
}
