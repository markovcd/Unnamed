namespace Tally.Core.Tests;

public sealed class TransactionBalancerTests
{
    private static AccountName Account(string name) => AccountName.Parse(name).Value;

    private static Amount Usd(decimal quantity) => new(quantity, "USD");

    private static DraftPosting Draft(string account, Amount? amount, Amount? assertion = null, int line = 1) =>
        new(Account(account), amount, assertion, line);

    [Fact]
    public void Postings_that_sum_to_zero_are_kept_as_written()
    {
        var result = TransactionBalancer.Resolve([Draft("a", Usd(5m)), Draft("b", Usd(-5m))]);

        Assert.True(result.IsOk);
        Assert.Equal([Usd(5m), Usd(-5m)], result.Value.Select(posting => posting.Amount));
    }

    [Fact]
    public void The_omitted_amount_is_the_negation_of_the_rest()
    {
        var result = TransactionBalancer.Resolve([Draft("a", Usd(5m)), Draft("b", Usd(2m)), Draft("c", null)]);

        Assert.Equal(Usd(-7m), result.Value[2].Amount);
    }

    [Fact]
    public void An_omitted_amount_in_a_transaction_of_two_commodities_becomes_one_posting_per_commodity()
    {
        var result = TransactionBalancer.Resolve([
            Draft("a", Usd(5m)),
            Draft("b", new Amount(3m, "EUR")),
            Draft("c", null),
        ]);

        Assert.Equal(
            [("a", Usd(5m)), ("b", new Amount(3m, "EUR")), ("c", new Amount(-3m, "EUR")), ("c", Usd(-5m))],
            result.Value.Select(posting => (posting.Account.Value, posting.Amount)));
    }

    [Fact]
    public void An_assertion_on_an_omitted_amount_rides_on_the_last_posting_it_expands_to()
    {
        var assertion = new Amount(1m, "USD");

        var result = TransactionBalancer.Resolve([
            Draft("a", Usd(5m)),
            Draft("b", new Amount(3m, "EUR")),
            Draft("c", null, assertion),
        ]);

        Assert.Equal([null, null, null, assertion], result.Value.Select(posting => posting.Assertion));
    }

    [Fact]
    public void An_omitted_amount_in_a_transaction_that_already_sums_to_zero_is_zero()
    {
        var result = TransactionBalancer.Resolve([Draft("a", Usd(5m)), Draft("b", Usd(-5m)), Draft("c", null)]);

        Assert.Equal(new Amount(0m, ""), result.Value[2].Amount);
    }

    [Fact]
    public void Postings_that_do_not_sum_to_zero_are_refused_with_the_difference()
    {
        var result = TransactionBalancer.Resolve([Draft("a", Usd(5m)), Draft("b", Usd(-4m))]);

        Assert.False(result.IsOk);
        Assert.Contains("1 USD", result.Error);
    }

    [Fact]
    public void Each_commodity_must_balance_on_its_own()
    {
        var result = TransactionBalancer.Resolve([Draft("a", Usd(5m)), Draft("b", new Amount(-5m, "EUR"))]);

        Assert.False(result.IsOk);
        Assert.Contains("5 USD", result.Error);
        Assert.Contains("-5 EUR", result.Error);
    }

    [Fact]
    public void Two_omitted_amounts_are_refused()
    {
        var result = TransactionBalancer.Resolve([Draft("a", null), Draft("b", null)]);

        Assert.False(result.IsOk);
        Assert.Contains("only one posting", result.Error);
    }
}
