namespace Tally.Core.Tests;

public sealed class AssertionCheckerTests
{
    private static string[] Problems(string text) =>
        [.. JournalParser.Parse("test.journal", text).Diagnostics.Select(diagnostic => diagnostic.ToString())];

    [Fact]
    public void An_assertion_that_matches_the_running_balance_passes()
    {
        var problems = Problems("""
            2026-01-01 open
                assets:cash  100 USD = 100 USD
                equity       -100 USD

            2026-01-02 spend
                assets:cash  -10 USD = 90 USD
                expenses     10 USD
            """);

        Assert.Empty(problems);
    }

    [Fact]
    public void A_failing_assertion_is_reported_at_its_posting_with_expected_and_actual()
    {
        var problems = Problems("""
            2026-01-01 open
                assets:cash  100 USD
                equity       -100 USD

            2026-01-02 spend
                assets:cash  -10 USD = 100 USD
                expenses     10 USD
            """);

        Assert.Equal(
            ["test.journal:6: balance assertion failed for assets:cash: expected 100 USD, actual 90 USD"],
            problems);
    }

    [Fact]
    public void Assertions_are_checked_in_date_order_not_source_order()
    {
        var problems = Problems("""
            2026-01-02 spend
                assets:cash  -10 USD = 90 USD
                expenses     10 USD

            2026-01-01 open
                assets:cash  100 USD
                equity       -100 USD
            """);

        Assert.Empty(problems);
    }

    [Fact]
    public void An_assertion_counts_only_its_own_commodity()
    {
        var problems = Problems("""
            2026-01-01 open
                assets:cash  100 USD
                assets:cash  50 EUR = 50 EUR
                equity       -100 USD
                equity       -50 EUR
            """);

        Assert.Empty(problems);
    }

    [Fact]
    public void An_assertion_covers_the_account_alone_not_its_children()
    {
        var problems = Problems("""
            2026-01-01 open
                assets:cash         10 USD = 10 USD
                assets:cash:wallet  5 USD
                equity              -15 USD
            """);

        Assert.Empty(problems);
    }

    [Fact]
    public void An_assertion_on_an_omitted_amount_checks_the_inferred_amount()
    {
        var problems = Problems("""
            2026-01-01 open
                equity       -100 USD
                assets:cash  = 100 USD
            """);

        Assert.Empty(problems);
    }

    [Fact]
    public void An_assertion_that_the_balance_is_zero_holds_for_an_untouched_account()
    {
        var problems = Problems("""
            2026-01-01 open
                assets:cash  1 USD
                equity       -1 USD

            2026-01-02 nothing
                assets:other  0 USD = 0 USD
                equity        0 USD
            """);

        Assert.Empty(problems);
    }
}
