using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Managers;
using FriendsCorner.Infrastructure.Accessors;
using FriendsCorner.Infrastructure.Utilities;
using Microsoft.Extensions.Time.Testing;

namespace FriendsCorner.Tests;

public class MysteryManagerTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 30, 20, 0, 0, TimeSpan.Zero));
    private readonly FakeSocket _a = new();
    private readonly FakeSocket _b = new();

    private async Task<(RoomManager Room, MysteryGame Game)> FailedRoom(FakeCaseWriter writer, MysterySettings? settings = null)
    {
        var room = new RoomManager(
            "room-1",
            () => { },
            new RoomEngine(),
            new SeatSocketAccessor(new WebSocketTextUtility()),
            new TickerUtility(),
            new StateMessageAccessor(),
            new NoRecorder(),
            writer,
            _time);
        var game = MysteryGame.Start([Seat.A, Seat.B], settings);
        game.TryFail(1);
        room.Restore(game);
        await room.Join(_a, CancellationToken.None);
        await room.Join(_b, CancellationToken.None);
        return (room, game);
    }

    // Rematch after a failure asks for case 2.
    private static Task Rematch(RoomManager room) => room.Act(Seat.A, new Rematch(), CancellationToken.None);

    [Fact]
    public async Task A_fair_case_starts_the_investigation()
    {
        var writer = new FakeCaseWriter(() => Task.FromResult<Case?>(CaseFixture.Fair()));
        var (room, game) = await FailedRoom(writer);

        await Rematch(room);
        await room.CaseWriting;

        Assert.Equal(MysteryPhase.Investigating, game.State.Phase);
        var asked = Assert.Single(writer.Asked);
        Assert.Contains(asked.Theme, CaseThemes.All);
        Assert.Equal(MysteryLevel.Easy, asked.Level);
    }

    [Fact]
    public async Task A_hard_game_asks_for_a_hard_case()
    {
        var writer = new FakeCaseWriter(() => Task.FromResult<Case?>(CaseFixture.Fair()));
        var (room, _) = await FailedRoom(writer, new MysterySettings(MysteryLevel.Hard, MysteryMode.Race));

        await Rematch(room);
        await room.CaseWriting;

        Assert.Equal(MysteryLevel.Hard, Assert.Single(writer.Asked).Level);
    }

    [Fact]
    public async Task The_new_case_is_broadcast_after_it_arrives()
    {
        var answer = new TaskCompletionSource<Case?>();
        var writer = new FakeCaseWriter(() => answer.Task);
        var (room, game) = await FailedRoom(writer);
        await Rematch(room);
        var (aBefore, bBefore) = (_a.Sent.Count, _b.Sent.Count);

        answer.SetResult(CaseFixture.Fair());
        await room.CaseWriting;

        Assert.Equal(MysteryPhase.Investigating, game.State.Phase);
        Assert.Equal(aBefore + 1, _a.Sent.Count);
        Assert.Equal(bBefore + 1, _b.Sent.Count);
    }

    [Fact]
    public async Task The_room_does_not_wait_for_the_writer()
    {
        var writer = new FakeCaseWriter(() => new TaskCompletionSource<Case?>().Task);
        var (room, game) = await FailedRoom(writer);

        await Rematch(room).WaitAsync(TimeSpan.FromSeconds(5));
        await room.Act(Seat.B, new ShareMedia(true, true), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(MysteryPhase.Writing, game.State.Phase);
        Assert.Single(writer.Asked);
    }

    [Fact]
    public async Task An_unfair_case_is_asked_for_once_more()
    {
        var unfair = CaseFixture.Fair() with { Proofs = [.. CaseFixture.Fair().Proofs, new Proof("s3", ["c4"])] };
        var writer = new FakeCaseWriter(
            () => Task.FromResult<Case?>(unfair),
            () => Task.FromResult<Case?>(CaseFixture.Fair()));
        var (room, game) = await FailedRoom(writer);

        await Rematch(room);
        await room.CaseWriting;

        Assert.Equal(2, writer.Asked.Count);
        Assert.Equal(MysteryPhase.Investigating, game.State.Phase);
    }

    [Fact]
    public async Task Two_unfair_cases_fail_after_exactly_two_asks()
    {
        var unfair = CaseFixture.Fair() with { Proofs = [] };
        var writer = new FakeCaseWriter(
            () => Task.FromResult<Case?>(unfair),
            () => Task.FromResult<Case?>(unfair),
            () => Task.FromResult<Case?>(CaseFixture.Fair()));
        var (room, game) = await FailedRoom(writer);

        await Rematch(room);
        await room.CaseWriting;

        Assert.Equal(2, writer.Asked.Count);
        Assert.Equal(MysteryPhase.Failed, game.State.Phase);
    }

    [Fact]
    public async Task No_case_twice_fails_after_exactly_two_asks()
    {
        var writer = new FakeCaseWriter(
            () => Task.FromResult<Case?>(null),
            () => Task.FromResult<Case?>(null));
        var (room, game) = await FailedRoom(writer);

        await Rematch(room);
        await room.CaseWriting;

        Assert.Equal(2, writer.Asked.Count);
        Assert.Equal(MysteryPhase.Failed, game.State.Phase);
    }

    [Fact]
    public async Task A_writer_that_throws_counts_as_no_case()
    {
        var writer = new FakeCaseWriter(
            () => throw new HttpRequestException("down"),
            () => Task.FromResult<Case?>(CaseFixture.Fair()));
        var (room, game) = await FailedRoom(writer);

        await Rematch(room);
        await room.CaseWriting;

        Assert.Equal(MysteryPhase.Investigating, game.State.Phase);
    }

    [Fact]
    public async Task Sixty_seconds_without_an_answer_fails_and_a_late_case_is_dropped()
    {
        var late = new TaskCompletionSource<Case?>();
        var writer = new FakeCaseWriter(() => late.Task);
        var (room, game) = await FailedRoom(writer);
        await Rematch(room);
        var (aBefore, bBefore) = (_a.Sent.Count, _b.Sent.Count);

        _time.Advance(TimeSpan.FromSeconds(59));
        Assert.Equal(MysteryPhase.Writing, game.State.Phase);
        _time.Advance(TimeSpan.FromSeconds(1));
        await room.CaseWriting;

        Assert.Equal(MysteryPhase.Failed, game.State.Phase);
        Assert.Single(writer.Asked);
        Assert.Equal(aBefore + 1, _a.Sent.Count);
        Assert.Equal(bBefore + 1, _b.Sent.Count);

        late.SetResult(CaseFixture.Fair());
        await Task.Delay(50);
        Assert.Equal(MysteryPhase.Failed, game.State.Phase);
    }

    [Fact]
    public async Task A_finished_countdown_asks_for_a_case()
    {
        var writer = new FakeCaseWriter(() => new TaskCompletionSource<Case?>().Task);
        var room = new RoomManager(
            "room-1",
            () => { },
            new RoomEngine(),
            new SeatSocketAccessor(new WebSocketTextUtility()),
            new TickerUtility(),
            new StateMessageAccessor(),
            new NoRecorder(),
            writer,
            _time);
        await room.Join(_a, CancellationToken.None);
        await room.Join(_b, CancellationToken.None);
        await room.Act(Seat.A, new PickGame(MysteryGame.GameId), CancellationToken.None);
        await room.Act(Seat.B, new SetReady(true), CancellationToken.None);
        await room.Act(Seat.A, new StartGame(), CancellationToken.None);

        _time.Advance(LobbyEngine.Countdown);
        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (writer.Asked.Count == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        Assert.Single(writer.Asked);
        await room.Leave(Seat.A);
        await room.Leave(Seat.B);
    }

    // Answers each ask with the next queued answer, then never answers.
    private sealed class FakeCaseWriter(params Func<Task<Case?>>[] answers) : ICaseWriterAccessor
    {
        private readonly Queue<Func<Task<Case?>>> _answers = new(answers);

        public List<CaseRequest> Asked { get; } = [];

        public Task<Case?> Write(CaseRequest request, CancellationToken cancellationToken)
        {
            lock (Asked)
            {
                Asked.Add(request);
            }

            return _answers.TryDequeue(out var answer) ? answer() : new TaskCompletionSource<Case?>().Task;
        }
    }

    private sealed class NoRecorder : IGameRecorderAccessor
    {
        public void Note(string roomId, IGameEngine game)
        {
        }
    }
}
