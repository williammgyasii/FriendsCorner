using FriendsCorner.Infrastructure.Accessors;

namespace FriendsCorner.Tests;

public class WordListAccessorTests
{
    private static readonly WordListAccessor Words = new();

    [Theory]
    [InlineData("cat")]
    [InlineData("CATS")]
    [InlineData("Zymurgy")]
    public void Real_words_are_in_the_list_in_any_case(string word)
    {
        Assert.True(Words.Contains(word));
    }

    [Theory]
    [InlineData("qxz")]
    [InlineData("")]
    [InlineData("TX")]
    public void Other_strings_are_not(string word)
    {
        Assert.False(Words.Contains(word));
    }
}
