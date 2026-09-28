namespace FriendsCorner.Tests;

public class GameSeamTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_game_move_reaches_the_running_game_through_the_seam()
    {
        var room = new RoomEngine();
        Assert.True(room.TryLaunch("chess"));

        room.Apply(Seat.A, new MoveChess(new ChessMove("e2", "e4")), Now);

        var chess = Assert.IsType<ChessGame>(room.Game);
        Assert.Equal(Seat.B, chess.Board.ToMove);
    }

    [Fact]
    public void A_move_for_a_game_that_is_not_running_changes_nothing()
    {
        var room = new RoomEngine();
        Assert.True(room.TryLaunch("tictactoe"));

        var change = room.Apply(Seat.A, new MoveChess(new ChessMove("e2", "e4")), Now);

        Assert.Equal(RoomChange.None, change);
        Assert.IsType<TicTacToeGame>(room.Game);
    }

    private static readonly PlacedTile[] Cat =
        [new(111, new Tile('C')), new(112, new Tile('A')), new(113, new Tile('T'))];

    private static LetterTilesGame TilesWithCat()
    {
        var words = new WordsAndScoreTests.FakeWords("CAT");
        var game = LetterTilesGame.Start([Seat.A, Seat.B], words, new Random(7));
        var racks = new Dictionary<Seat, IReadOnlyList<Tile>>
        {
            [Seat.A] = "CATSEEN".Select(letter => new Tile(letter)).ToArray(),
            [Seat.B] = "QXZBDFG".Select(letter => new Tile(letter)).ToArray(),
        };
        return new LetterTilesGame(game.State with { Racks = racks }, words, new Random(7));
    }

    [Fact]
    public void Games_without_questions_answer_nothing()
    {
        IGameEngine marks = new TicTacToeGame(Board.Empty());
        IGameEngine chess = new ChessGame(ChessBoard.Start(Seat.A));

        Assert.Null(marks.Ask(Seat.A, new PreviewTiles(Cat)));
        Assert.Null(chess.Ask(Seat.A, new PreviewTiles(Cat)));
    }

    [Fact]
    public void Letter_tiles_answers_a_preview_with_the_asked_tiles()
    {
        var game = TilesWithCat();

        var answer = Assert.IsType<TilesPreview>(game.Ask(Seat.A, new PreviewTiles(Cat)));

        Assert.Equal(Cat, answer.Tiles);
        Assert.Equal(["CAT"], answer.Words);
        Assert.Equal(10, answer.Score);
    }

    [Fact]
    public void Letter_tiles_answers_a_refused_preview_with_the_reason()
    {
        var game = TilesWithCat();

        var answer = Assert.IsType<TilesPreviewRefused>(game.Ask(Seat.B, new PreviewTiles(Cat)));

        Assert.Equal(Cat, answer.Tiles);
        Assert.Equal(RefusalReason.NotInRack, answer.Refusal.Reason);
        Assert.Null(game.RefusalFor(Seat.B));
    }

    [Fact]
    public void A_seat_that_is_not_playing_gets_no_answer()
    {
        Assert.Null(TilesWithCat().Ask(Seat.C, new PreviewTiles(Cat)));
    }

    [Fact]
    public void A_room_with_no_game_answers_nothing()
    {
        Assert.Null(new RoomEngine().Ask(Seat.A, new PreviewTiles(Cat)));
    }
}
