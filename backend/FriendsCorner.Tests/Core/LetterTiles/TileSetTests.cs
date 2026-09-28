namespace FriendsCorner.Tests;

public class TileSetTests
{
    [Fact]
    public void A_fresh_bag_holds_100_tiles_worth_187()
    {
        var bag = TileSet.FullBag();

        Assert.Equal(100, bag.Count);
        Assert.Equal(12, bag.Count(tile => tile == new Tile('E')));
        Assert.Equal(2, bag.Count(tile => tile.IsBlank));
        Assert.Equal(187, bag.Sum(TileSet.ValueOf));
    }

    [Theory]
    [InlineData("AEILNORSTU", 1)]
    [InlineData("DG", 2)]
    [InlineData("BCMP", 3)]
    [InlineData("FHVWY", 4)]
    [InlineData("K", 5)]
    [InlineData("JX", 8)]
    [InlineData("QZ", 10)]
    public void Each_letter_has_its_value(string letters, int value)
    {
        Assert.All(letters, letter => Assert.Equal(value, TileSet.ValueOf(new Tile(letter))));
    }

    [Fact]
    public void A_blank_is_worth_nothing_even_when_it_stands_for_a_letter()
    {
        Assert.Equal(0, TileSet.ValueOf(Tile.Blank));
        Assert.Equal(0, TileSet.ValueOf(Tile.Blank.As('Q')));
    }

    [Fact]
    public void The_same_randomness_shuffles_the_same_bag()
    {
        var first = TileSet.ShuffledBag(new Random(7));
        var second = TileSet.ShuffledBag(new Random(7));

        Assert.Equal(first, second);
        Assert.NotEqual(TileSet.FullBag(), first);
        Assert.Equal(TileSet.FullBag().Order(), first.Order());
    }
}
