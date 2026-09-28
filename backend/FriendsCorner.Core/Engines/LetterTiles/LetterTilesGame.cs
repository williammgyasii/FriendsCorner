using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines.Games;

namespace FriendsCorner.Core.Engines.LetterTiles;

// Adds to the rules what the room needs: which seat was last refused and why.
// A refusal stays until that seat's next accepted move, so the seat sees it
// on the next broadcast without the room needing a separate "explain" change.
public sealed class LetterTilesGame(LetterTilesState state, IWordListAccessor words, Random random) : IGameEngine
{
    public const string GameId = "tiles";
    public const int MinPlayers = 2;

    private readonly Dictionary<Seat, Refusal> _refusals = new();

    public string Id => GameId;

    public LetterTilesState State { get; private set; } = state;

    public static LetterTilesGame Start(IReadOnlyList<Seat> playing, IWordListAccessor words, Random random) =>
        new(LetterTilesState.Start(playing, playing[0], random), words, random);

    public Refusal? RefusalFor(Seat seat) => _refusals.GetValueOrDefault(seat);

    public bool TryPlay(Seat seat, GameMove move)
    {
        LetterTilesState next;
        Refusal? refusal;
        var accepted = move switch
        {
            PlayTiles play => State.TryPlay(seat, play.Tiles, words, out next, out refusal),
            ExchangeTiles exchange => State.TryExchange(seat, exchange.Tiles, random, out next, out refusal),
            PassTurn => State.TryPass(seat, out next, out refusal),
            _ => Ignored(out next, out refusal),
        };

        if (!accepted)
        {
            if (refusal is not null)
            {
                _refusals[seat] = refusal;
            }

            return false;
        }

        State = next;
        _refusals.Remove(seat);
        return true;
    }

    public bool TryRematch()
    {
        if (!State.TryRematch(random, out var next))
        {
            return false;
        }

        State = next;
        _refusals.Clear();
        return true;
    }

    public GameAnswer? Ask(Seat seat, GameQuestion question)
    {
        if (question is not PreviewTiles preview || !State.Playing.Contains(seat))
        {
            return null;
        }

        return State.TryPreview(seat, preview.Tiles, words, out var judgement, out var refusal)
            ? new TilesPreview(preview.Tiles, judgement.Words, judgement.Score)
            : new TilesPreviewRefused(preview.Tiles, refusal!);
    }

    private bool Ignored(out LetterTilesState next, out Refusal? refusal)
    {
        next = State;
        refusal = null;
        return false;
    }
}
