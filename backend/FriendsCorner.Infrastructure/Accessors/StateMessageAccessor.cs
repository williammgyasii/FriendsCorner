using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;
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
            board = room.TicTacToe is null
                ? null
                : new
                {
                    squares = room.TicTacToe.Squares.Select(square => square?.ToString()).ToArray(),
                    next = room.TicTacToe.Next.ToString(),
                    winner = room.TicTacToe.Winner?.ToString(),
                    draw = room.TicTacToe.IsDraw,
                },
            chess = room.Chess is not { } chess
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
                    ready = lobby.IsReady(seat),
                    camera = lobby.MediaOf(seat).Camera,
                    mic = lobby.MediaOf(seat).Mic,
                    playing = playing.Contains(seat),
                }).ToArray(),
            },
        });
    }
}
