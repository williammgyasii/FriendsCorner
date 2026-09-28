namespace FriendsCorner.Tests;

public class ChessBoardTests
{
    private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    [Fact]
    public void A_new_game_puts_seat_A_on_white_to_move_with_twenty_moves()
    {
        var board = ChessBoard.Start();

        Assert.Equal(Seat.A, board.White);
        Assert.Equal(Seat.A, board.ToMove);
        Assert.Equal(StartFen, board.Fen);
        Assert.Equal(20, board.LegalMoves.Count);
        Assert.Null(board.Outcome);
        Assert.False(board.InCheck);
    }

    // Perft counts every line of play to a fixed depth. The expected numbers are the
    // published ones (chessprogramming.org/Perft_Results); matching them proves the
    // library underneath handles castling, en passant, promotion, and pins.
    [Theory]
    [InlineData(StartFen, 1, 20)]
    [InlineData(StartFen, 2, 400)]
    [InlineData(StartFen, 3, 8_902)]
    [InlineData("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 1, 48)]
    [InlineData("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 2, 2_039)]
    [InlineData("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 3, 2_812)]
    [InlineData("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 2, 264)]
    public void Legal_move_counts_match_the_published_perft_numbers(string fen, int depth, long expected)
    {
        Assert.True(ChessBoard.TryFromFen(fen, out var board));

        Assert.Equal(expected, Perft(board, depth));
    }

    [Fact]
    public void A_legal_move_passes_the_turn_to_black()
    {
        Assert.True(ChessBoard.Start().TryMove(Seat.A, new ChessMove("e2", "e4"), out var board));

        Assert.Equal(Seat.B, board.ToMove);
        Assert.StartsWith("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b", board.Fen);
    }

    [Fact]
    public void The_waiting_seat_cannot_move()
    {
        Assert.False(ChessBoard.Start().TryMove(Seat.B, new ChessMove("e7", "e5"), out var board));

        Assert.Equal(StartFen, board.Fen);
    }

    [Fact]
    public void An_illegal_move_is_rejected()
    {
        Assert.False(ChessBoard.Start().TryMove(Seat.A, new ChessMove("e2", "e5"), out var board));

        Assert.Equal(StartFen, board.Fen);
    }

    [Fact]
    public void Fools_mate_ends_the_game_with_black_winning()
    {
        var board = Play(ChessBoard.Start(), ("f2", "f3"), ("e7", "e5"), ("g2", "g4"), ("d8", "h4"));

        Assert.True(board.InCheck);
        Assert.Equal(new ChessOutcome(ChessEnding.Checkmate, Seat.B), board.Outcome);
        Assert.Empty(board.LegalMoves);
        Assert.False(board.TryMove(Seat.A, new ChessMove("a2", "a3"), out _));
    }

    [Fact]
    public void No_legal_moves_without_check_is_stalemate()
    {
        Assert.True(ChessBoard.TryFromFen("7k/5Q2/6K1/8/8/8/8/8 b - - 0 1", out var board));

        Assert.False(board.InCheck);
        Assert.Equal(new ChessOutcome(ChessEnding.Stalemate, null), board.Outcome);
    }

    [Fact]
    public void A_pawn_reaching_the_last_rank_needs_a_chosen_piece()
    {
        Assert.True(ChessBoard.TryFromFen("8/P7/8/8/8/8/8/k6K w - - 0 1", out var board));

        Assert.False(board.TryMove(Seat.A, new ChessMove("a7", "a8"), out _));
        Assert.True(board.TryMove(Seat.A, new ChessMove("a7", "a8", 'n'), out var promoted));
        Assert.StartsWith("N7/", promoted.Fen);
    }

    [Fact]
    public void A_broken_position_is_refused()
    {
        Assert.False(ChessBoard.TryFromFen("not a chess position", out _));
    }

    [Fact]
    public void Rematch_is_only_allowed_after_the_game_ends()
    {
        Assert.False(ChessBoard.Start().TryRematch(out _));
    }

    [Fact]
    public void Rematch_starts_over_with_colors_swapped()
    {
        var finished = Play(ChessBoard.Start(), ("f2", "f3"), ("e7", "e5"), ("g2", "g4"), ("d8", "h4"));

        Assert.True(finished.TryRematch(out var rematch));

        Assert.Equal(StartFen, rematch.Fen);
        Assert.Equal(Seat.B, rematch.White);
        Assert.Equal(Seat.B, rematch.ToMove);
        Assert.Null(rematch.Outcome);
    }

    private static long Perft(ChessBoard board, int depth)
    {
        if (depth == 0)
        {
            return 1;
        }

        long total = 0;
        foreach (var move in board.LegalMoves)
        {
            Assert.True(board.TryMove(board.ToMove, move, out var next));
            total += Perft(next, depth - 1);
        }

        return total;
    }

    private static ChessBoard Play(ChessBoard board, params (string From, string To)[] moves)
    {
        foreach (var (from, to) in moves)
        {
            Assert.True(board.TryMove(board.ToMove, new ChessMove(from, to), out board));
        }

        return board;
    }
}
