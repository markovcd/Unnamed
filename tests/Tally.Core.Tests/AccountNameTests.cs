namespace Tally.Core.Tests;

public sealed class AccountNameTests
{
    private static AccountName Name(string text) => AccountName.Parse(text).Value;

    [Theory]
    [InlineData("assets")]
    [InlineData("assets:bank:checking")]
    [InlineData("expenses:food-and_drink")]
    [InlineData("assets:2026")]
    [InlineData("équité:épargne")]
    public void A_path_of_plain_segments_is_an_account_name(string text)
    {
        Assert.Equal(text, Name(text).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(":assets")]
    [InlineData("assets:")]
    [InlineData("assets::cash")]
    [InlineData("assets cash")]
    [InlineData("assets:ca$h")]
    [InlineData("assets/cash")]
    public void A_malformed_path_is_refused_with_a_reason(string text)
    {
        var name = AccountName.Parse(text);

        Assert.False(name.IsOk);
        Assert.NotEmpty(name.Error);
    }

    [Fact]
    public void A_name_longer_than_the_limit_is_refused()
    {
        Assert.False(AccountName.Parse(new string('a', 257)).IsOk);
        Assert.True(AccountName.Parse(new string('a', 256)).IsOk);
    }

    [Theory]
    [InlineData("assets:cash", "assets", true)]
    [InlineData("assets:cash", "assets:cash", true)]
    [InlineData("assets:cash:wallet", "assets:cash", true)]
    [InlineData("assets:cash", "ass", false)]
    [InlineData("assets:cash", "assets:ca", false)]
    [InlineData("assets", "assets:cash", false)]
    public void An_account_is_within_another_by_whole_segments(string account, string ancestor, bool expected)
    {
        Assert.Equal(expected, Name(account).IsWithin(Name(ancestor)));
    }

    [Theory]
    [InlineData("assets:bank:checking", 1, "assets")]
    [InlineData("assets:bank:checking", 2, "assets:bank")]
    [InlineData("assets:bank:checking", 9, "assets:bank:checking")]
    public void Truncating_keeps_the_leading_segments(string account, int depth, string expected)
    {
        Assert.Equal(expected, Name(account).Truncate(depth).Value);
    }
}
