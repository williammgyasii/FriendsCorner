using DistanceTogether;
using DistanceTogether.Server;

namespace DistanceTogether.Tests;

public class BoardRecorderTests
{
    [Fact]
    public void A_later_snapshot_replaces_one_still_waiting()
    {
        var recorder = new BoardRecorder();
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
    public async Task A_move_is_shown_before_the_write_returns()
    {
        var releaseWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Board? written = null;
        var recorder = new BoardRecorder(async (_, board) =>
        {
            await releaseWrite.Task;
            written = board;
        });

        var room = new Room();
        Assert.True(room.TryAddSeat(out Seat seat));
        Assert.True(room.TryLaunch("tictactoe"));
        Assert.True(room.TryPlace(seat, 0));

        var shown = false;
        var shownAndNoted = recorder.ShowThenRecord("room-1", room, () => shown = true);
        var completed = await Task.WhenAny(shownAndNoted, Task.Delay(200));

        Assert.Same(shownAndNoted, completed);
        Assert.True(shown);
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
        var room = new Room();
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
