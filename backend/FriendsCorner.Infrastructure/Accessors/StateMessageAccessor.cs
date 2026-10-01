using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;
using FriendsCorner.Core.Engines.Games;
using FriendsCorner.Core.Engines.LetterTiles;
using FriendsCorner.Core.Engines.Mystery;
using System.Text.Json;

namespace FriendsCorner.Infrastructure.Accessors;

// The shape of what each browser is sent. The page's store reads exactly
// these names, so a change here is a change to the frontend contract.
public sealed class StateMessageAccessor : IStateMessageAccessor
{
    public string Joined(Seat seat) => JsonSerializer.Serialize(new { type = "joined", seat = seat.ToString() });

    public string Write(RoomEngine room, Seat you, DateTimeOffset now)
    {
        object? Player(Seat seat) =>
            room.Positions.TryGetValue(seat, out var position)
                ? new { x = position.X, y = position.Y }
                : null;

        var lobby = room.Lobby;
        var playing = lobby.Playing;

        return JsonSerializer.Serialize(new
        {
            type = "state",
            you = you.ToString(),
            world = room.World,
            board = room.Game is not TicTacToeGame { Board: var board }
                ? null
                : new
                {
                    squares = board.Squares.Select(square => square?.ToString()).ToArray(),
                    next = board.Next.ToString(),
                    winner = board.Winner?.ToString(),
                    draw = board.IsDraw,
                },
            chess = room.Game is not ChessGame { Board: var chess }
                ? null
                : new
                {
                    fen = chess.Fen,
                    white = chess.White.ToString(),
                    toMove = chess.ToMove.ToString(),
                    inCheck = chess.InCheck,
                    lastMove = chess.LastMove is { } last ? new { from = last.From, to = last.To } : null,
                    outcome = chess.Outcome is not { } outcome
                        ? null
                        : new
                        {
                            ending = outcome.Ending == ChessEnding.Checkmate ? "checkmate" : "stalemate",
                            winner = outcome.Winner?.ToString(),
                        },
                    legalMoves = chess.LegalMoves
                        .Select(move => new { from = move.From, to = move.To, promotion = move.Promotion?.ToString() })
                        .ToArray(),
                },
            tiles = room.Game is LetterTilesGame tiles ? Tiles(tiles, you) : null,
            mystery = room.Game is MysteryGame mystery ? Mystery(mystery.View(you), now) : null,
            players = new { A = Player(Seat.A), B = Player(Seat.B), C = Player(Seat.C), D = Player(Seat.D) },
            lobby = new
            {
                host = lobby.Host?.ToString(),
                capacity = lobby.Capacity,
                pick = lobby.Pick,
                canStart = lobby.CanStart,
                countdownMs = lobby.CountdownEndsAt is { } endsAt
                    ? Math.Max(0, (int)Math.Ceiling((endsAt - now).TotalMilliseconds))
                    : (int?)null,
                members = lobby.Players.Select(seat => new
                {
                    seat = seat.ToString(),
                    gameName = lobby.GameNameOf(seat),
                    ready = lobby.IsReady(seat),
                    camera = lobby.MediaOf(seat).Camera,
                    mic = lobby.MediaOf(seat).Mic,
                    playing = playing.Contains(seat),
                }).ToArray(),
                mystery = new { level = Lower(lobby.Mystery.Level), mode = Lower(lobby.Mystery.Mode) },
            },
        });
    }

    private static readonly Dictionary<string, int> LetterValues =
        Enumerable.Range('A', 26)
            .Select(letter => new Tile((char)letter))
            .Append(Tile.Blank)
            .ToDictionary(tile => tile.Letter.ToString(), TileSet.ValueOf);

    // Built for one seat: its own rack, and only counts for everyone else.
    // The bag goes out as a number so its order never leaves the server.
    private static object Tiles(LetterTilesGame game, Seat you)
    {
        var state = game.State;
        return new
        {
            layout = PremiumLayout.Text,
            board = state.Board
                .Select(tile => tile is { } placed ? (placed.IsBlank ? char.ToLowerInvariant(placed.Letter) : placed.Letter).ToString() : null)
                .ToArray(),
            values = LetterValues,
            players = state.Playing
                .Select(seat => new { seat = seat.ToString(), score = state.Scores[seat], count = state.Racks[seat].Count })
                .ToArray(),
            toMove = state.ToMove.ToString(),
            bag = state.Bag.Count,
            rack = state.Racks.TryGetValue(you, out var rack)
                ? rack.Select(tile => tile.Letter.ToString()).ToArray()
                : [],
            lastPlay = state.LastPlay is { } last
                ? new { seat = last.Seat.ToString(), words = last.Words, score = last.Score }
                : null,
            outcome = state.Outcome is { } outcome
                ? new
                {
                    winners = outcome.Winners.Select(seat => seat.ToString()).ToArray(),
                    scores = state.Playing.ToDictionary(seat => seat.ToString(), seat => state.Scores[seat]),
                }
                : null,
            refusal = game.RefusalFor(you) is { } refusal
                ? new { reason = ReasonCode(refusal.Reason), words = refusal.Words }
                : null,
        };
    }

    // Written only from one seat's view, which has no place for a secret.
    // Clocks go out as milliseconds left, like the lobby countdown.
    private static object Mystery(MysteryView view, DateTimeOffset now) => new
    {
        phase = Lower(view.Phase),
        level = Lower(view.Settings.Level),
        mode = Lower(view.Settings.Mode),
        mood = view.Mood is { } mood ? Lower(mood) : null,
        title = view.Title,
        setting = view.Setting,
        victim = view.Victim is { } victim ? new { name = victim.Name, found = victim.Found } : null,
        suspects = view.Suspects
            .Select(suspect => new { id = suspect.Id, name = suspect.Name, role = suspect.Role, motive = suspect.Motive, alibi = suspect.Alibi })
            .ToArray(),
        places = view.Places.Select(place => new { id = place.Id, name = place.Name }).ToArray(),
        leads = view.Leads
            .Select(lead => new { id = lead.Id, kind = Lower(lead.Kind), about = lead.About, text = lead.Text, by = lead.By?.ToString() })
            .ToArray(),
        leadsLeft = view.LeadsLeft,
        picks = new { A = view.Picks.GetValueOrDefault(Seat.A), B = view.Picks.GetValueOrDefault(Seat.B) },
        @out = view.Out.Select(seat => seat.ToString()).ToArray(),
        endsInMs = MillisecondsLeft(view.EndsAt, now),
        locksInMs = MillisecondsLeft(view.LocksAt, now),
        outcome = view.Outcome is { } outcome
            ? new
            {
                killer = outcome.Killer,
                how = outcome.How,
                why = outcome.Why,
                story = outcome.Story,
                accused = outcome.Accused,
                right = outcome.Right,
                score = outcome.Score,
                winner = outcome.Winner?.ToString(),
                timedOut = outcome.TimedOut,
            }
            : null,
    };

    private static int? MillisecondsLeft(DateTimeOffset? at, DateTimeOffset now) =>
        at is { } then ? Math.Max(0, (int)Math.Ceiling((then - now).TotalMilliseconds)) : null;

    private static string Lower<T>(T value) where T : struct, Enum => value.ToString().ToLowerInvariant();

    public string? Answer(GameAnswer answer) => answer switch
    {
        TilesPreview preview => JsonSerializer.Serialize(new
        {
            type = "tiles-preview",
            tiles = Asked(preview.Tiles),
            words = preview.Words,
            score = preview.Score,
        }),
        TilesPreviewRefused refused => JsonSerializer.Serialize(new
        {
            type = "tiles-preview",
            tiles = Asked(refused.Tiles),
            refusal = new { reason = ReasonCode(refused.Refusal.Reason), words = refused.Refusal.Words },
        }),
        _ => null,
    };

    private static object[] Asked(IReadOnlyList<PlacedTile> tiles) =>
        tiles.Select(tile => (object)new { square = tile.Square, letter = tile.Tile.Letter.ToString(), blank = tile.Tile.IsBlank }).ToArray();

    private static string ReasonCode(RefusalReason reason) => reason switch
    {
        RefusalReason.NotYourTurn => "not-your-turn",
        RefusalReason.NotInRack => "not-in-rack",
        RefusalReason.NotInLine => "not-in-line",
        RefusalReason.Gap => "gap",
        RefusalReason.FirstMustCoverCentre => "first-must-cover-centre",
        RefusalReason.FirstNeedsTwoTiles => "first-needs-two-tiles",
        RefusalReason.NotConnected => "not-connected",
        RefusalReason.NotAWord => "not-a-word",
        RefusalReason.BagTooSmall => "bag-too-small",
        _ => "game-over",
    };
}
