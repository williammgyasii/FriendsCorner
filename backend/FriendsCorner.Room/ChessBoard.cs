using pax.chess;
using pax.chess.Validation;

namespace FriendsCorner;

public readonly record struct ChessMove(string From, string To, char? Promotion = null);

public enum ChessEnding
{
    Checkmate,
    Stalemate,
}

public sealed record ChessOutcome(ChessEnding Ending, Seat? Winner);

// Our own face over pax.chess: the room and page only ever see these types,
// so the library can be swapped without touching them.
public sealed class ChessBoard
{
    private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    private static readonly char[] PromotionPieces = ['q', 'r', 'b', 'n'];

    private readonly BoardPosition _position;
    private readonly Dictionary<ChessMove, Move> _legal;

    private ChessBoard(BoardPosition position, Seat white, ChessMove? lastMove = null)
    {
        _position = position;
        White = white;
        LastMove = lastMove;
        _legal = LegalMovesOf(position);
        LegalMoves = _legal.Keys.ToList();

        var state = PseudoMoveValidator.GetGameState(position);
        InCheck = state is GameState.Check or GameState.Checkmate;
        Outcome = state switch
        {
            GameState.Checkmate => new ChessOutcome(ChessEnding.Checkmate, Other(ToMove)),
            GameState.Stalemate => new ChessOutcome(ChessEnding.Stalemate, null),
            _ => null,
        };
    }

    public Seat White { get; }

    public Seat ToMove => _position.SideToMove == PieceColor.White ? White : Other(White);

    public string Fen => FenSerializer.Serialize(_position);

    public IReadOnlyList<ChessMove> LegalMoves { get; }

    public bool InCheck { get; }

    public ChessOutcome? Outcome { get; }

    // Not part of FEN, so a game revived from Neon starts without one.
    public ChessMove? LastMove { get; }

    public static ChessBoard Start(Seat white = Seat.A) => new(FenSerializer.Parse(StartFen), white);

    public static bool TryFromFen(string fen, out ChessBoard board) => TryFromFen(fen, Seat.A, out board);

    public static bool TryFromFen(string fen, Seat white, out ChessBoard board)
    {
        board = null!;
        try
        {
            var position = FenSerializer.Parse(fen);
            if (position.Board.WhiteKingSquare is null || position.Board.BlackKingSquare is null)
            {
                return false;
            }

            board = new ChessBoard(position, white);
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return false;
        }
    }

    public bool TryMove(Seat seat, ChessMove move, out ChessBoard updated)
    {
        updated = this;
        var normalized = Normalize(move);
        if (Outcome is not null || seat != ToMove || !_legal.TryGetValue(normalized, out var legal))
        {
            return false;
        }

        updated = new ChessBoard(_position.MakeMove(legal), White, normalized);
        return true;
    }

    public bool TryRematch(out ChessBoard updated)
    {
        updated = this;
        if (Outcome is null)
        {
            return false;
        }

        updated = Start(Other(White));
        return true;
    }

    private static Dictionary<ChessMove, Move> LegalMovesOf(BoardPosition position)
    {
        var moves = new Dictionary<ChessMove, Move>();
        foreach (var (square, _) in position.Board.GetPieces(position.SideToMove).ToList())
        {
            foreach (var move in PseudoMoveValidator.GetValidMoves(square, position, out _))
            {
                var from = move.From.ToString();
                var to = move.To.ToString();
                if (!move.MoveType.HasFlag(MoveType.Promotion))
                {
                    moves[new ChessMove(from, to)] = move;
                    continue;
                }

                foreach (var piece in PromotionPieces)
                {
                    moves[new ChessMove(from, to, piece)] = move with { Promotion = FenSerializer.GetPieceType(piece) };
                }
            }
        }

        return moves;
    }

    private static ChessMove Normalize(ChessMove move) =>
        new(move.From.ToLowerInvariant(), move.To.ToLowerInvariant(), move.Promotion is { } piece ? char.ToLowerInvariant(piece) : null);

    private static Seat Other(Seat seat) => seat == Seat.A ? Seat.B : Seat.A;
}
