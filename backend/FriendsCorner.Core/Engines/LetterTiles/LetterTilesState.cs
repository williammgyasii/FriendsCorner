using FriendsCorner.Core.Accessors;

namespace FriendsCorner.Core.Engines.LetterTiles;

public sealed record LastPlay(Seat Seat, IReadOnlyList<string> Words, int Score);

// The words a placement forms and what it scores.
public sealed record Judgement(IReadOnlyList<string> Words, int Score)
{
    public static readonly Judgement None = new([], 0);
}

// The final scores are the state's Scores.
public sealed record TilesOutcome(IReadOnlyList<Seat> Winners);

// One whole game as a value. Every rule returns a new state or a refusal,
// so a refused command leaves the game exactly as it was.
public sealed record LetterTilesState(
    IReadOnlyList<Tile?> Board,
    IReadOnlyList<Tile> Bag,
    IReadOnlyDictionary<Seat, IReadOnlyList<Tile>> Racks,
    IReadOnlyDictionary<Seat, int> Scores,
    IReadOnlyList<Seat> Playing,
    Seat ToMove,
    Seat Starter,
    int ScorelessTurns,
    LastPlay? LastPlay,
    TilesOutcome? Outcome)
{
    public const int RackSize = 7;
    public const int ScorelessTurnsToEnd = 6;

    public static LetterTilesState Start(IReadOnlyList<Seat> playing, Seat starter, Random random)
    {
        var bag = TileSet.ShuffledBag(random).ToList();
        var racks = new Dictionary<Seat, IReadOnlyList<Tile>>();
        foreach (var seat in playing)
        {
            racks[seat] = bag.Take(RackSize).ToArray();
            bag.RemoveRange(0, RackSize);
        }

        return new LetterTilesState(
            new Tile?[PremiumLayout.Squares],
            bag,
            racks,
            playing.ToDictionary(seat => seat, _ => 0),
            playing,
            starter,
            starter,
            ScorelessTurns: 0,
            LastPlay: null,
            Outcome: null);
    }

    // What a play would score, without playing it. Whose turn it is does not
    // matter, so a waiting player can plan.
    public bool TryPreview(Seat seat, IReadOnlyList<PlacedTile> placed, IWordListAccessor wordList, out Judgement judgement, out Refusal? refusal)
    {
        judgement = Judgement.None;
        if (Outcome is not null)
        {
            refusal = new Refusal(RefusalReason.GameOver);
            return false;
        }

        return TryJudge(seat, placed, wordList, out judgement, out refusal);
    }

    public bool TryPlay(Seat seat, IReadOnlyList<PlacedTile> placed, IWordListAccessor wordList, out LetterTilesState next, out Refusal? refusal)
    {
        next = this;
        if ((refusal = TurnRefusal(seat)) is not null)
        {
            return false;
        }

        if (!TryJudge(seat, placed, wordList, out var judgement, out refusal))
        {
            return false;
        }

        var score = judgement.Score;
        var board = Board.ToArray();
        foreach (var tile in placed)
        {
            board[tile.Square] = tile.Tile;
        }

        Placement.TryTake(Racks[seat], placed.Select(tile => tile.Tile.IsBlank ? Tile.Blank : tile.Tile), out var left);
        var bag = Bag.ToList();
        var drawn = Math.Min(RackSize - left.Count, bag.Count);
        left.AddRange(bag.Take(drawn));
        bag.RemoveRange(0, drawn);

        next = this with
        {
            Board = board,
            Bag = bag,
            Racks = With(Racks, seat, left),
            Scores = With(Scores, seat, Scores[seat] + score),
            LastPlay = new LastPlay(seat, judgement.Words, score),
        };
        next = left.Count == 0 ? next.GoneOut(seat) : next.EndTurn(scored: score > 0);
        return true;
    }

    private bool TryJudge(Seat seat, IReadOnlyList<PlacedTile> placed, IWordListAccessor wordList, out Judgement judgement, out Refusal? refusal)
    {
        judgement = Judgement.None;
        refusal = null;
        if (!Racks.TryGetValue(seat, out var rack))
        {
            refusal = new Refusal(RefusalReason.NotInRack);
            return false;
        }

        if (Placement.Check(Board, rack, placed) is { } reason)
        {
            refusal = new Refusal(reason);
            return false;
        }

        var words = WordFinder.Find(Board, placed);
        if (WordFinder.Unknown(words, wordList) is { Count: > 0 } unknown)
        {
            refusal = new Refusal(RefusalReason.NotAWord, unknown);
            return false;
        }

        judgement = new Judgement(words.Select(word => word.Text).ToArray(), Scorer.Score(Board, placed, words));
        return true;
    }

    // The player draws before the swapped tiles go back, so they cannot draw
    // their own tiles straight back.
    public bool TryExchange(Seat seat, IReadOnlyList<Tile> tiles, Random random, out LetterTilesState next, out Refusal? refusal)
    {
        next = this;
        if ((refusal = TurnRefusal(seat)) is not null)
        {
            return false;
        }

        if (Bag.Count < RackSize)
        {
            refusal = new Refusal(RefusalReason.BagTooSmall);
            return false;
        }

        if (tiles.Count == 0 || !Placement.TryTake(Racks[seat], tiles, out var left))
        {
            refusal = new Refusal(RefusalReason.NotInRack);
            return false;
        }

        left.AddRange(Bag.Take(tiles.Count));
        var bag = TileSet.Shuffled(Bag.Skip(tiles.Count).Concat(tiles), random);
        next = (this with { Bag = bag, Racks = With(Racks, seat, left) }).EndTurn(scored: false);
        return true;
    }

    public bool TryPass(Seat seat, out LetterTilesState next, out Refusal? refusal)
    {
        next = this;
        if ((refusal = TurnRefusal(seat)) is not null)
        {
            return false;
        }

        next = EndTurn(scored: false);
        return true;
    }

    public bool TryRematch(Random random, out LetterTilesState next)
    {
        next = this;
        if (Outcome is null)
        {
            return false;
        }

        next = Start(Playing, After(Starter), random);
        return true;
    }

    private Refusal? TurnRefusal(Seat seat) =>
        Outcome is not null ? new Refusal(RefusalReason.GameOver)
        : seat != ToMove ? new Refusal(RefusalReason.NotYourTurn)
        : null;

    private LetterTilesState EndTurn(bool scored)
    {
        var scoreless = scored ? 0 : ScorelessTurns + 1;
        if (scoreless >= ScorelessTurnsToEnd)
        {
            var settled = Playing.ToDictionary(seat => seat, seat => Scores[seat] - RackValue(seat));
            return Ended(this with { ScorelessTurns = scoreless }, settled);
        }

        return this with { ScorelessTurns = scoreless, ToMove = After(ToMove) };
    }

    private LetterTilesState GoneOut(Seat seat)
    {
        var others = Playing.Where(other => other != seat).ToArray();
        var settled = Playing.ToDictionary(
            player => player,
            player => player == seat ? Scores[player] + others.Sum(RackValue) : Scores[player] - RackValue(player));
        return Ended(this with { ScorelessTurns = 0 }, settled);
    }

    private static LetterTilesState Ended(LetterTilesState state, Dictionary<Seat, int> scores)
    {
        var best = scores.Values.Max();
        var winners = state.Playing.Where(seat => scores[seat] == best).ToArray();
        return state with { Scores = scores, Outcome = new TilesOutcome(winners) };
    }

    private int RackValue(Seat seat) => Racks[seat].Sum(TileSet.ValueOf);

    private Seat After(Seat seat) => Playing[(IndexOf(seat) + 1) % Playing.Count];

    private int IndexOf(Seat seat)
    {
        for (var i = 0; i < Playing.Count; i++)
        {
            if (Playing[i] == seat)
            {
                return i;
            }
        }

        return -1;
    }

    private static Dictionary<Seat, T> With<T>(IReadOnlyDictionary<Seat, T> map, Seat seat, T value) =>
        new(map) { [seat] = value };
}
