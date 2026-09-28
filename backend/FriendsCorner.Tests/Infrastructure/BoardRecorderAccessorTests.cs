using FriendsCorner.Core.Accessors;
using FriendsCorner.Infrastructure.Accessors;

namespace FriendsCorner.Tests;

public class BoardRecorderAccessorTests
{
    [Fact]
    public void A_later_snapshot_replaces_one_still_waiting()
    {
        var recorder = new BoardRecorderAccessor();
        var earlier = Play(0);
        var later = Play(0, 3);
        var otherRoom = Play(4);

        recorder.Note("room-1", earlier);
        recorder.Note("room-2", otherRoom);
        recorder.Note("room-1", later);

        var waiting = recorder.TakeWaiting().OrderBy(snapshot => snapshot.RoomId).ToArray();

        Assert.Equal(2, waiting.Length);
        Assert.Equal("room-1", waiting[0].RoomId);
        Assert.Equal('X', waiting[0].Board.Squares[0]);
        Assert.Equal('O', waiting[0].Board.Squares[3]);
        Assert.Equal(Seat.A, waiting[0].Board.Next);
        Assert.Equal("room-2", waiting[1].RoomId);
        Assert.Equal('X', waiting[1].Board.Squares[4]);
    }

    [Fact]
    public async Task Noting_a_move_returns_before_the_write_finishes()
    {
        var releaseWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Board? written = null;
        var recorder = new BoardRecorderAccessor(async (_, board) =>
        {
            await releaseWrite.Task;
            written = board;
        });

        recorder.Note("room-1", Play(0));

        Assert.Null(written);

        releaseWrite.TrySetResult();
        var quiet = recorder.WhenQuiet();
        var finished = await Task.WhenAny(quiet, Task.Delay(1000));
        Assert.Same(quiet, finished);
        Assert.Equal('X', written!.Squares[0]);
        Assert.Equal(Seat.B, written.Next);
    }

    private static Board Play(params int[] squares)
    {
        var room = new RoomEngine();
        Assert.True(room.TryAddSeat(out Seat _));
        Assert.True(room.TryAddSeat(out Seat _));
        Assert.True(room.TryLaunch("tictactoe"));
        foreach (var square in squares)
        {
            var seat = room.TicTacToe!.Next;
            Assert.True(room.TryPlace(seat, square));
        }

        return room.TicTacToe!;
    }
}
