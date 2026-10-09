namespace Tally.Core.Tests;

public sealed class RegisterReportTests
{
    private const string Text = """
        2026-01-01 Opening
            assets:cash  100 USD
            equity

        2026-01-10 Groceries
            expenses:food  40 USD
            assets:cash

        2026-01-15 Dinner
            expenses:food  25 USD
            assets:cash
        """;

    private static Journal Read()
    {
        var result = JournalParser.Parse("test.journal", Text);
        Assert.True(result.IsValid, string.Join('\n', result.Diagnostics));
        return result.Journal;
    }

    [Fact]
    public void Every_posting_is_a_row_with_the_running_balance_after_it()
    {
        var report = RegisterReport.Build(Read(), new PostingFilter(AccountName.Parse("assets:cash").Value));

        Assert.Equal(
            [
                ("2026-01-01", "Opening", "100 USD", "100 USD"),
                ("2026-01-10", "Groceries", "-40 USD", "60 USD"),
                ("2026-01-15", "Dinner", "-25 USD", "35 USD"),
            ],
            report.Rows.Select(row => (row.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), row.Description, row.Amount.ToString(), row.Running.ToString())));
    }

    [Fact]
    public void Without_a_filter_the_running_balance_of_the_whole_journal_is_zero_after_each_transaction()
    {
        var report = RegisterReport.Build(Read(), PostingFilter.None);

        Assert.Equal(6, report.Rows.Length);
        Assert.True(report.Rows[1].Running.IsZero);
        Assert.True(report.Rows[^1].Running.IsZero);
    }

    [Fact]
    public void A_date_range_starts_the_running_balance_from_zero()
    {
        var filter = new PostingFilter(AccountName.Parse("assets:cash").Value, From: new DateOnly(2026, 1, 10));

        var report = RegisterReport.Build(Read(), filter);

        Assert.Equal(["-40 USD", "-65 USD"], report.Rows.Select(row => row.Running.ToString()));
    }
}
