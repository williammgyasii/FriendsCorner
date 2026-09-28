namespace FriendsCorner.Tests;

public class PremiumLayoutTests
{
    private static readonly int[] Squares = Enumerable.Range(0, 225).ToArray();

    [Theory]
    [InlineData(Premium.TripleWord, 8)]
    [InlineData(Premium.DoubleWord, 17)]
    [InlineData(Premium.TripleLetter, 12)]
    [InlineData(Premium.DoubleLetter, 24)]
    public void The_board_has_the_right_number_of_each_premium(Premium premium, int count)
    {
        Assert.Equal(count, Squares.Count(square => PremiumLayout.At(square) == premium));
    }

    [Theory]
    [InlineData(0, Premium.TripleWord)]
    [InlineData(7, Premium.TripleWord)]
    [InlineData(14, Premium.TripleWord)]
    [InlineData(105, Premium.TripleWord)]
    [InlineData(119, Premium.TripleWord)]
    [InlineData(210, Premium.TripleWord)]
    [InlineData(217, Premium.TripleWord)]
    [InlineData(224, Premium.TripleWord)]
    [InlineData(112, Premium.DoubleWord)]
    [InlineData(20, Premium.TripleLetter)]
    [InlineData(24, Premium.TripleLetter)]
    [InlineData(3, Premium.DoubleLetter)]
    [InlineData(11, Premium.DoubleLetter)]
    [InlineData(1, Premium.None)]
    public void Landmark_squares(int square, Premium premium)
    {
        Assert.Equal(premium, PremiumLayout.At(square));
    }

    [Fact]
    public void The_layout_looks_the_same_rotated_or_mirrored()
    {
        static int Rotate(int square) => (square % 15 * 15) + (14 - (square / 15));
        static int Mirror(int square) => (square / 15 * 15) + (14 - (square % 15));

        Assert.All(Squares, square =>
        {
            Assert.Equal(PremiumLayout.At(square), PremiumLayout.At(Rotate(square)));
            Assert.Equal(PremiumLayout.At(square), PremiumLayout.At(Mirror(square)));
        });
    }

    [Fact]
    public void The_layout_text_is_one_character_per_square_with_the_centre_starred()
    {
        Assert.Equal(225, PremiumLayout.Text.Length);
        Assert.Equal('*', PremiumLayout.Text[PremiumLayout.Centre]);
        Assert.Equal("T..d...T...d..T", PremiumLayout.Text[..15]);
    }
}
