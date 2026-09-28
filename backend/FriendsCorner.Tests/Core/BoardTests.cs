namespace FriendsCorner.Tests;

public class BoardTests
{
    [Fact]
    public void A_new_board_has_nine_empty_squares_and_seat_A_to_play()
    {
        var board = Board.Empty();

        Assert.Equal(9, board.Squares.Count);
        Assert.All(board.Squares, square => Assert.Null(square));
        Assert.Equal(Seat.A, board.Next);
    }

    [Fact]
    public void Seat_A_places_and_then_it_is_seat_B()
    {
        Assert.True(Board.Empty().TryPlace(Seat.A, 0, out var board));

        Assert.Equal('X', board.Squares[0]);
        Assert.Equal(Seat.B, board.Next);
        Assert.Null(board.Winner);
        Assert.False(board.IsDraw);
    }

    [Fact]
    public void The_waiting_seat_cannot_place()
    {
        Assert.False(Board.Empty().TryPlace(Seat.B, 0, out var board));

        Assert.All(board.Squares, square => Assert.Null(square));
        Assert.Equal(Seat.A, board.Next);
    }

    [Fact]
    public void A_filled_square_rejects_the_next_mark()
    {
        Assert.True(Board.Empty().TryPlace(Seat.A, 0, out var board));

        Assert.False(board.TryPlace(Seat.B, 0, out board));
        Assert.Equal('X', board.Squares[0]);
    }

    [Fact]
    public void Three_in_a_row_ends_the_game()
    {
        var board = Board.Empty();
        Assert.True(board.TryPlace(Seat.A, 0, out board));
        Assert.True(board.TryPlace(Seat.B, 3, out board));
        Assert.True(board.TryPlace(Seat.A, 1, out board));
        Assert.True(board.TryPlace(Seat.B, 4, out board));
        Assert.True(board.TryPlace(Seat.A, 2, out board));

        Assert.Equal(Seat.A, board.Winner);
        Assert.False(board.TryPlace(Seat.B, 5, out _));
    }

    [Fact]
    public void A_full_board_with_no_line_is_a_draw()
    {
        var board = Board.Empty();
        int[] squares = [0, 1, 2, 4, 3, 5, 7, 6, 8];
        foreach (var square in squares)
        {
            var seat = board.Next;
            Assert.True(board.TryPlace(seat, square, out board));
        }

        Assert.Null(board.Winner);
        Assert.True(board.IsDraw);
    }

    [Fact]
    public void A_finished_board_can_be_played_again()
    {
        AssertCleared(FinishedWin());
        AssertCleared(FinishedDraw());
    }

    [Fact]
    public void A_rematch_in_the_middle_of_a_game_is_refused()
    {
        Assert.True(Board.Empty().TryPlace(Seat.A, 0, out var board));

        Assert.False(board.TryRematch(out var again));
        Assert.Equal('X', again.Squares[0]);
        Assert.Equal(Seat.B, again.Next);
    }

    private static void AssertCleared(Board board)
    {
        Assert.True(board.TryRematch(out var again));
        Assert.Equal(9, again.Squares.Count);
        Assert.All(again.Squares, square => Assert.Null(square));
        Assert.Equal(Seat.A, again.Next);
        Assert.Null(again.Winner);
        Assert.False(again.IsDraw);
    }

    private static Board FinishedWin()
    {
        var board = Board.Empty();
        Assert.True(board.TryPlace(Seat.A, 0, out board));
        Assert.True(board.TryPlace(Seat.B, 3, out board));
        Assert.True(board.TryPlace(Seat.A, 1, out board));
        Assert.True(board.TryPlace(Seat.B, 4, out board));
        Assert.True(board.TryPlace(Seat.A, 2, out board));
        return board;
    }

    private static Board FinishedDraw()
    {
        var board = Board.Empty();
        int[] squares = [0, 1, 2, 4, 3, 5, 7, 6, 8];
        foreach (var square in squares)
        {
            Assert.True(board.TryPlace(board.Next, square, out board));
        }

        return board;
    }
}
