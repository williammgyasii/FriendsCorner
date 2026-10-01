namespace FriendsCorner.Tests;

public class AccountEngineTests
{
    [Fact]
    public void Email_comparison_ignores_case_and_surrounding_spaces()
    {
        Assert.Equal(
            AccountEngine.NormalizeEmail("Ada@x.com"),
            AccountEngine.NormalizeEmail(" ada@x.com "));
    }

    [Fact]
    public void A_name_is_trimmed_and_kept_up_to_40_characters()
    {
        Assert.Equal("Ada Lovelace", AccountEngine.AcceptName("  Ada Lovelace  "));
        Assert.Equal("Café", AccountEngine.AcceptName("Café"));
        Assert.Null(AccountEngine.AcceptName("   "));
        Assert.Null(AccountEngine.AcceptName(new string('a', 41)));
        Assert.Equal(new string('a', 40), AccountEngine.AcceptName(new string('a', 40)));
    }

    [Fact]
    public void A_password_shorter_than_8_characters_is_refused()
    {
        Assert.False(AccountEngine.AcceptsPassword("short"));
        Assert.True(AccountEngine.AcceptsPassword("correct-horse"));
    }

    [Fact]
    public void An_unknown_email_and_a_wrong_password_are_the_same_rejection()
    {
        var unknown = AccountEngine.SignIn(userId: null, passwordMatches: true);
        var wrong = AccountEngine.SignIn(userId: Guid.NewGuid(), passwordMatches: false);

        Assert.Equal(unknown, wrong);
        Assert.Null(unknown.Id);
    }

    [Fact]
    public void A_match_returns_the_existing_guid()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var decision = AccountEngine.SignIn(id, passwordMatches: true);

        Assert.Equal(id, decision.Id);
    }
}
