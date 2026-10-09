namespace Tally.Core.Tests;

public sealed class JournalPrinterTests
{
    private static Journal Read(string text)
    {
        var result = JournalParser.Parse("test.journal", text);
        Assert.True(result.IsValid, string.Join('\n', result.Diagnostics));
        return result.Journal;
    }

    [Fact]
    public void A_journal_prints_in_canonical_form()
    {
        var journal = Read("""
            2026-01-05   *   Grocery store ; note
              expenses:food  12.50   USD
              assets:cash

            2026-01-01 !
                assets:cash  100 USD = 100 USD
                equity
            """);

        Assert.Equal(
            """
            format 1

            2026-01-01 !
                assets:cash   100 USD = 100 USD
                equity       -100 USD

            2026-01-05 * Grocery store
                expenses:food   12.50 USD
                assets:cash    -12.50 USD

            """,
            JournalPrinter.Print(journal));
    }

    [Fact]
    public void A_printed_journal_reads_back_as_the_same_journal()
    {
        var journal = Read("""
            2026-02-01 * Mixed
                assets:bank:checking   500 USD
                assets:bank:checking   20 EUR
                equity:opening        -500 USD
                equity:opening

            2026-01-15 ! Pending thing
                expenses:misc  0.07 USD = 0.07 USD
                assets:cash

            2026-01-16
                a  1 USD
                b
            """);

        var printed = JournalPrinter.Print(journal);

        Assert.Equal(printed, JournalPrinter.Print(Read(printed)));
        Assert.Equal(Summaries(journal), Summaries(Read(printed)));
    }

    [Fact]
    public void A_printed_empty_journal_is_only_its_format_line()
    {
        Assert.Equal("format 1\n", JournalPrinter.Print(Journal.Empty));
    }

    private static string[] Summaries(Journal journal) =>
    [
        .. journal.Transactions.Select(transaction =>
            $"{transaction.Date:yyyy-MM-dd}|{transaction.Status}|{transaction.Description}|"
            + string.Join(';', transaction.Postings.Select(posting => $"{posting.Account} {posting.Amount} {posting.Assertion}"))),
    ];
}
