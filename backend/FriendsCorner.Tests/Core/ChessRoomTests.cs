using FriendsCorner.Core.Accessors;
using FriendsCorner.Infrastructure.Accessors;

namespace FriendsCorner.Tests;

public class ChessRoomTests
{
    [Fact]
    public void Launching_chess_sets_up_a_new_game()
    {
        var room = new RoomEngine();

        Assert.True(room.TryLaunch("chess"));

        Assert.Equal("chess", room.World);
        Assert.NotNull(room.Chess);
        Assert.Equal(Seat.A, room.Chess.White);
        Assert.Null(room.TicTacToe);
    }

    [Fact]
    public void A_chess_room_cannot_become_tic_tac_toe()
    {
        var room = new RoomEngine();
        Assert.True(room.TryLaunch("chess"));

        Assert.False(room.TryLaunch("tictactoe"));
        Assert.Null(room.TicTacToe);
    }

    [Fact]
    public void The_seat_to_move_moves_and_the_other_is_refused()
    {
        var room = new RoomEngine();
        Assert.True(room.TryLaunch("chess"));

        Assert.False(room.TryChessMove(Seat.B, new ChessMove("e7", "e5")));
        Assert.True(room.TryChessMove(Seat.A, new ChessMove("e2", "e4")));
        Assert.Equal(Seat.B, room.Chess!.ToMove);
    }

    [Fact]
    public void Chess_rematch_waits_for_the_game_to_end()
    {
        var room = new RoomEngine();
        Assert.True(room.TryLaunch("chess"));
        Assert.False(room.TryChessRematch());

        PlayFoolsMate(room);

        Assert.True(room.TryChessRematch());
        Assert.Equal(Seat.B, room.Chess!.White);
    }

    [Fact]
    public void A_saved_chess_game_can_be_restored_into_a_room()
    {
        Assert.True(ChessBoard.TryFromFen(
            "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1", Seat.B, out var saved));
        var room = new RoomEngine();

        room.RestoreChess(saved);

        Assert.Equal("chess", room.World);
        Assert.Equal(Seat.B, room.Chess!.White);
        Assert.Equal(Seat.A, room.Chess.ToMove);
    }

    [Fact]
    public void The_last_move_is_remembered_so_the_page_can_highlight_it()
    {
        Assert.Null(ChessBoard.Start().LastMove);
        Assert.True(ChessBoard.Start().TryMove(Seat.A, new ChessMove("e2", "e4"), out var board));

        Assert.Equal(new ChessMove("e2", "e4"), board.LastMove);
    }

    [Fact]
    public void A_chess_move_command_moves_a_piece()
    {
        var room = new RoomEngine();
        Assert.True(room.TryAddSeat(out Seat seat));
        Assert.True(room.TryLaunch("chess"));

        var change = room.Apply(seat, new MoveChess(new ChessMove("e2", "e4")), Now);

        Assert.Equal(RoomChange.Chess, change);
        Assert.Equal(Seat.B, room.Chess!.ToMove);
    }

    [Fact]
    public void A_chess_move_command_carries_the_promotion_choice()
    {
        var room = new RoomEngine();
        Assert.True(ChessBoard.TryFromFen("8/P7/8/8/8/8/8/k6K w - - 0 1", out var board));
        room.RestoreChess(board);

        var change = room.Apply(Seat.A, new MoveChess(new ChessMove("a7", "a8", 'n')), Now);

        Assert.Equal(RoomChange.Chess, change);
        Assert.StartsWith("N7/", room.Chess!.Fen);
    }

    [Fact]
    public void A_chess_rematch_command_starts_over_after_the_end()
    {
        var room = new RoomEngine();
        Assert.True(room.TryLaunch("chess"));
        PlayFoolsMate(room);

        var change = room.Apply(Seat.A, new ChessRematch(), Now);

        Assert.Equal(RoomChange.Chess, change);
        Assert.Null(room.Chess!.Outcome);
    }

    private static readonly DateTimeOffset Now = new(2026, 9, 27, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_later_chess_snapshot_replaces_one_still_waiting()
    {
        var recorder = new ChessRecorderAccessor();
        Assert.True(ChessBoard.Start().TryMove(Seat.A, new ChessMove("e2", "e4"), out var later));

        recorder.Note("room-1", ChessBoard.Start());
        recorder.Note("room-1", later);

        var waiting = Assert.Single(recorder.TakeWaiting());
        Assert.Equal("room-1", waiting.RoomId);
        Assert.Equal(later.Fen, waiting.Board.Fen);
    }

    private static void PlayFoolsMate(RoomEngine room)
    {
        foreach (var (from, to) in new[] { ("f2", "f3"), ("e7", "e5"), ("g2", "g4"), ("d8", "h4") })
        {
            Assert.True(room.TryChessMove(room.Chess!.ToMove, new ChessMove(from, to)));
        }
    }
}
