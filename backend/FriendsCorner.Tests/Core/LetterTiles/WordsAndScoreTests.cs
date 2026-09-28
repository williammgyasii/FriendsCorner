using FriendsCorner.Core.Accessors;

namespace FriendsCorner.Tests;

public class WordsAndScoreTests
{
    private static Tile?[] EmptyBoard() => new Tile?[PremiumLayout.Squares];

    private static PlacedTile[] Across(int start, string letters) =>
        letters.Select((letter, i) => new PlacedTile(start + i, new Tile(letter))).ToArray();

    private static Tile?[] WithCat()
    {
        var board = EmptyBoard();
        foreach (var placed in Across(111, "CAT"))
        {
            board[placed.Square] = placed.Tile;
        }

        return board;
    }

    [Fact]
    public void Cat_forms_one_word()
    {
        var words = WordFinder.Find(EmptyBoard(), Across(111, "CAT"));

        Assert.Equal(["CAT"], words.Select(word => word.Text));
    }

    [Fact]
    public void An_s_on_the_end_hooks_cats()
    {
        var words = WordFinder.Find(WithCat(), [new PlacedTile(114, new Tile('S'))]);

        Assert.Equal(["CATS"], words.Select(word => word.Text));
    }

    [Fact]
    public void A_tile_under_the_t_forms_a_cross_word()
    {
        var words = WordFinder.Find(WithCat(), [new PlacedTile(128, new Tile('X'))]);

        Assert.Equal(["TX"], words.Select(word => word.Text));
        Assert.Equal(["TX"], WordFinder.Unknown(words, new FakeWords("CAT", "CATS")));
    }

    [Fact]
    public void An_unknown_main_word_is_named()
    {
        var words = WordFinder.Find(EmptyBoard(), Across(111, "QXZ"));

        Assert.Equal(["QXZ"], WordFinder.Unknown(words, new FakeWords("CAT")));
    }

    [Fact]
    public void Known_words_leave_nothing_unknown()
    {
        var words = WordFinder.Find(EmptyBoard(), Across(111, "CAT"));

        Assert.Empty(WordFinder.Unknown(words, new FakeWords("cat")));
    }

    [Fact]
    public void Cat_on_the_centre_scores_10()
    {
        var play = Across(111, "CAT");

        Assert.Equal(10, Scorer.Score(EmptyBoard(), play, WordFinder.Find(EmptyBoard(), play)));
    }

    [Fact]
    public void The_centre_premium_does_not_count_again()
    {
        PlacedTile[] play = [new(114, new Tile('S'))];

        Assert.Equal(6, Scorer.Score(WithCat(), play, WordFinder.Find(WithCat(), play)));
    }

    [Fact]
    public void A_blank_scores_nothing()
    {
        PlacedTile[] play = [new(111, new Tile('C')), new(112, Tile.Blank.As('A')), new(113, new Tile('T'))];

        Assert.Equal(8, Scorer.Score(EmptyBoard(), play, WordFinder.Find(EmptyBoard(), play)));
    }

    [Fact]
    public void Placing_all_seven_tiles_adds_50()
    {
        var play = Across(109, "SEATING");

        Assert.Equal(16 + 50, Scorer.Score(EmptyBoard(), play, WordFinder.Find(EmptyBoard(), play)));
    }

    [Fact]
    public void A_letter_premium_under_a_new_tile_counts_once_per_word()
    {
        // 108 is a double letter (row 7, column 3), so HAT is 4 x 2 + 1 + 1.
        var board = EmptyBoard();
        board[109] = new Tile('A');
        board[110] = new Tile('T');
        PlacedTile[] play = [new(108, new Tile('H'))];

        Assert.Equal(10, Scorer.Score(board, play, WordFinder.Find(board, play)));
    }

    internal sealed class FakeWords(params string[] words) : IWordListAccessor
    {
        private readonly HashSet<string> _words = new(words, StringComparer.OrdinalIgnoreCase);

        public bool Contains(string word) => _words.Contains(word);
    }
}
