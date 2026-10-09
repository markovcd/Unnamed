namespace Tally.Core.Tests;

public sealed class AmountTests
{
    private static Amount Read(string text) => Amount.Parse(text).Value;

    [Fact]
    public void A_number_followed_by_a_commodity_is_an_amount()
    {
        var amount = Read("12.50 USD");

        Assert.Equal(12.50m, amount.Quantity);
        Assert.Equal("USD", amount.Commodity);
    }

    [Fact]
    public void A_bare_number_is_an_amount_with_no_commodity()
    {
        var amount = Read("7");

        Assert.Equal(7m, amount.Quantity);
        Assert.Equal("", amount.Commodity);
    }

    [Fact]
    public void A_commodity_may_touch_the_number()
    {
        Assert.Equal(new Amount(3m, "EUR"), Read("3EUR"));
    }

    [Fact]
    public void A_commodity_with_digits_must_be_set_apart_so_scientific_notation_is_not_misread()
    {
        Assert.Contains("space", Amount.Parse("1e5").Error);
        Assert.Equal(new Amount(1m, "e5"), Read("1 e5"));
    }

    [Theory]
    [InlineData("-4.25 USD", -4.25)]
    [InlineData("+4.25 USD", 4.25)]
    public void A_sign_is_part_of_the_quantity(string text, double expected)
    {
        Assert.Equal((decimal)expected, Read(text).Quantity);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("USD 12")]
    [InlineData("1.")]
    [InlineData(".5")]
    [InlineData("1.2.3")]
    [InlineData("--1")]
    [InlineData("-")]
    [InlineData("1e5")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1,000 USD")]
    [InlineData("12 US D")]
    [InlineData("12 3USD")]
    [InlineData("12 US$")]
    [InlineData("12 ABCDEFGHIJKLMNOPQ")]
    public void Text_that_is_not_an_amount_is_refused_with_a_reason(string text)
    {
        var amount = Amount.Parse(text);

        Assert.False(amount.IsOk);
        Assert.NotEmpty(amount.Error);
    }

    [Theory]
    [InlineData("1000000000000.01 USD")]
    [InlineData("-1000000000000.01 USD")]
    [InlineData("79228162514264337593543950336")]
    [InlineData("99999999999999999999999999999999999999")]
    public void A_quantity_beyond_the_ceiling_is_refused_instead_of_overflowing(string text)
    {
        var amount = Amount.Parse(text);

        Assert.False(amount.IsOk);
        Assert.Contains("range", amount.Error);
    }

    [Fact]
    public void A_quantity_at_the_ceiling_is_accepted()
    {
        Assert.Equal(Amount.MaxQuantity, Read("1000000000000 USD").Quantity);
    }

    [Theory]
    [InlineData("12.50 USD")]
    [InlineData("-3 EUR")]
    [InlineData("0.000001 BTC")]
    [InlineData("42")]
    public void A_printed_amount_reads_back_as_the_same_amount(string text)
    {
        var amount = Read(text);

        Assert.Equal(text, amount.ToString());
        Assert.Equal(amount, Read(amount.ToString()));
    }
}
