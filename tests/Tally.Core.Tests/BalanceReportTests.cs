namespace Tally.Core.Tests;

public sealed class BalanceReportTests
{
    private const string Household = """
        2026-01-01 * Opening
            assets:bank:checking   1000 USD
            assets:cash             100 USD
            equity:opening

        2026-01-10 * Groceries
            expenses:food:groceries   40 USD
            assets:cash

        2026-02-03 * Lunch
            expenses:food:restaurants   15.50 USD
            assets:bank:checking

        2026-02-20 * Gift from abroad
            assets:bank:checking   25 EUR
            income:gifts          -25 EUR
        """;

    private static Journal Read(string text)
    {
        var result = JournalParser.Parse("test.journal", text);
        Assert.True(result.IsValid, string.Join('\n', result.Diagnostics));
        return result.Journal;
    }

    private static PostingFilter FilterOf(string? account = null, string? from = null, string? to = null) => new(
        account is null ? null : AccountName.Parse(account).Value,
        from is null ? null : DateOnly.Parse(from, System.Globalization.CultureInfo.InvariantCulture),
        to is null ? null : DateOnly.Parse(to, System.Globalization.CultureInfo.InvariantCulture));

    private static string[] Lines(BalanceReport report) =>
        [.. report.Rows.Select(row => $"{row.Account}: {row.Balance}")];

    [Fact]
    public void Each_account_shows_what_it_holds_in_account_order()
    {
        var report = BalanceReport.Build(Read(Household), PostingFilter.None);

        Assert.Equal(
            [
                "assets:bank:checking: 25 EUR, 984.50 USD",
                "assets:cash: 60 USD",
                "equity:opening: -1100 USD",
                "expenses:food:groceries: 40 USD",
                "expenses:food:restaurants: 15.50 USD",
                "income:gifts: -25 EUR",
            ],
            Lines(report));
    }

    [Fact]
    public void The_total_of_a_whole_journal_is_zero()
    {
        Assert.True(BalanceReport.Build(Read(Household), PostingFilter.None).Total.IsZero);
    }

    [Fact]
    public void An_account_filter_keeps_that_account_and_the_ones_beneath_it()
    {
        var report = BalanceReport.Build(Read(Household), FilterOf(account: "expenses:food"));

        Assert.Equal(["expenses:food:groceries: 40 USD", "expenses:food:restaurants: 15.50 USD"], Lines(report));
        Assert.Equal("55.50 USD", report.Total.ToString());
    }

    [Fact]
    public void A_date_range_keeps_both_of_its_ends()
    {
        var report = BalanceReport.Build(Read(Household), FilterOf(account: "expenses", from: "2026-01-10", to: "2026-02-03"));

        Assert.Equal("55.50 USD", report.Total.ToString());
    }

    [Fact]
    public void A_date_range_excludes_what_falls_outside_it()
    {
        var report = BalanceReport.Build(Read(Household), FilterOf(account: "expenses", from: "2026-01-11", to: "2026-02-02"));

        Assert.Empty(report.Rows);
        Assert.True(report.Total.IsZero);
    }

    [Fact]
    public void A_depth_rolls_deeper_accounts_up_into_their_ancestor()
    {
        var report = BalanceReport.Build(Read(Household), PostingFilter.None, depth: 2);

        Assert.Equal(
            [
                "assets:bank: 25 EUR, 984.50 USD",
                "assets:cash: 60 USD",
                "equity:opening: -1100 USD",
                "expenses:food: 55.50 USD",
                "income:gifts: -25 EUR",
            ],
            Lines(report));
    }

    [Fact]
    public void An_account_that_nets_to_zero_still_has_a_row()
    {
        var report = BalanceReport.Build(Read("""
            2026-01-01 in
                a  5 USD
                b
            2026-01-02 out
                a  -5 USD
                b
            """), PostingFilter.None);

        Assert.Equal(["a: 0", "b: 0"], Lines(report));
    }
}
