namespace Tally.Core.Tests;

public sealed class BalanceTests
{
    private static Amount Usd(decimal quantity) => new(quantity, "USD");

    private static Amount Eur(decimal quantity) => new(quantity, "EUR");

    [Fact]
    public void Amounts_of_one_commodity_add_up()
    {
        var balance = Balance.Empty.Plus(Usd(10m)).Plus(Usd(2.5m));

        Assert.Equal(12.5m, balance.Of("USD"));
    }

    [Fact]
    public void Commodities_are_held_apart_and_listed_in_order()
    {
        var balance = Balance.Empty.Plus(Usd(1m)).Plus(Eur(2m));

        Assert.Equal([Eur(2m), Usd(1m)], balance.Amounts);
    }

    [Fact]
    public void A_commodity_that_sums_to_zero_is_gone()
    {
        var balance = Balance.Empty.Plus(Usd(5m)).Plus(Usd(-5m));

        Assert.True(balance.IsZero);
        Assert.Empty(balance.Amounts);
    }

    [Fact]
    public void Adding_a_balance_adds_every_commodity()
    {
        var left = Balance.Empty.Plus(Usd(1m)).Plus(Eur(1m));
        var right = Balance.Empty.Plus(Usd(2m));

        var sum = left.Plus(right);

        Assert.Equal(3m, sum.Of("USD"));
        Assert.Equal(1m, sum.Of("EUR"));
    }

    [Fact]
    public void A_balance_plus_its_negation_is_zero()
    {
        var balance = Balance.Empty.Plus(Usd(4m)).Plus(Eur(-9m));

        Assert.True(balance.Plus(balance.Negated()).IsZero);
    }

    [Fact]
    public void Adding_never_changes_the_balance_it_started_from()
    {
        var start = Balance.Empty.Plus(Usd(1m));

        _ = start.Plus(Usd(1m));

        Assert.Equal(1m, start.Of("USD"));
    }

    [Fact]
    public void An_empty_balance_prints_as_zero_and_others_list_their_amounts()
    {
        Assert.Equal("0", Balance.Empty.ToString());
        Assert.Equal("2 EUR, 1 USD", Balance.Empty.Plus(Usd(1m)).Plus(Eur(2m)).ToString());
    }
}
