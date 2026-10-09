namespace Tally.Core.Tests;

public sealed class JournalParserTests
{
    private static ParseResult Parse(string text) => JournalParser.Parse("test.journal", text);

    private static string[] Problems(string text) => [.. Parse(text).Diagnostics.Select(diagnostic => diagnostic.ToString())];

    [Fact]
    public void A_balanced_transaction_is_read_with_its_date_description_and_postings()
    {
        var result = Parse("""
            2026-01-05 Grocery store
                expenses:food    12.50 USD
                assets:cash     -12.50 USD
            """);

        var transaction = Assert.Single(result.Journal.Transactions);
        Assert.True(result.IsValid);
        Assert.Equal(new DateOnly(2026, 1, 5), transaction.Date);
        Assert.Equal("Grocery store", transaction.Description);
        Assert.Equal(
            [("expenses:food", 12.50m), ("assets:cash", -12.50m)],
            transaction.Postings.Select(posting => (posting.Account.Value, posting.Amount.Quantity)));
    }

    [Fact]
    public void An_omitted_amount_is_filled_in_so_the_transaction_balances()
    {
        var result = Parse("""
            2026-01-05 Coffee
                expenses:food    3.20 USD
                assets:cash
            """);

        Assert.Equal(-3.20m, result.Journal.Transactions[0].Postings[1].Amount.Quantity);
    }

    [Fact]
    public void A_posting_remembers_the_line_it_came_from()
    {
        var result = Parse("""
            ; a comment

            2026-01-05 Coffee
                expenses:food    3.20 USD
                assets:cash
            """);

        var transaction = result.Journal.Transactions[0];
        Assert.Equal(3, transaction.Line);
        Assert.Equal([4, 5], transaction.Postings.Select(posting => posting.Line));
    }

    [Theory]
    [InlineData("2026-01-05 * Paid", "Cleared", "Paid")]
    [InlineData("2026-01-05 ! Waiting", "Pending", "Waiting")]
    [InlineData("2026-01-05 Plain", "Unmarked", "Plain")]
    [InlineData("2026-01-05 *Starred", "Unmarked", "*Starred")]
    [InlineData("2026-01-05 *", "Cleared", "")]
    [InlineData("2026-01-05", "Unmarked", "")]
    [InlineData("2026-01-05 * Paid ; with a note", "Cleared", "Paid")]
    public void A_header_carries_an_optional_status_mark_and_description(string header, string status, string description)
    {
        var result = Parse(header + "\n    a  1 USD\n    b  -1 USD\n");

        var transaction = Assert.Single(result.Journal.Transactions);
        Assert.Equal(status, transaction.Status.ToString());
        Assert.Equal(description, transaction.Description);
    }

    [Fact]
    public void Comments_and_blank_lines_are_ignored_anywhere()
    {
        var result = Parse("""
            # top comment
            ; another

            2026-01-05 Coffee ; trailing note
                ; a comment between postings
                expenses:food    3.20 USD   ; why
                assets:cash

            ; between transactions
            """);

        Assert.True(result.IsValid);
        Assert.Single(result.Journal.Transactions);
    }

    [Fact]
    public void Transactions_are_ordered_by_date_and_ties_keep_the_order_of_the_source()
    {
        var result = Parse("""
            2026-03-01 third
                a  1 USD
                b
            2026-01-01 first
                a  1 USD
                b

            2026-01-01 second
                a  1 USD
                b
            """);

        Assert.Equal(["first", "second", "third"], result.Journal.Transactions.Select(transaction => transaction.Description));
    }

    [Fact]
    public void Windows_line_endings_read_the_same_as_unix_ones()
    {
        const string unix = "2026-01-05 x\n    a  1 USD\n    b\n";

        var windows = Parse(unix.Replace("\n", "\r\n", StringComparison.Ordinal));

        Assert.True(windows.IsValid);
        Assert.Equal(JournalPrinter.Print(Parse(unix).Journal), JournalPrinter.Print(windows.Journal));
    }

    [Fact]
    public void An_empty_journal_is_valid()
    {
        var result = Parse("");

        Assert.True(result.IsValid);
        Assert.Empty(result.Journal.Transactions);
    }

    [Theory]
    [InlineData("2026-13-01 x", "does not start with a date")]
    [InlineData("2026-02-30 x", "does not start with a date")]
    [InlineData("2026-1-5 x", "does not start with a date")]
    [InlineData("2026-01-05x", "does not start with a date")]
    [InlineData("12 monkeys", "does not start with a date")]
    public void A_bad_date_is_reported_at_its_line_and_its_postings_are_not_blamed_again(string header, string reason)
    {
        var problems = Problems(header + "\n    a  1 USD\n    b\n");

        var problem = Assert.Single(problems);
        Assert.StartsWith("test.journal:1: ", problem);
        Assert.Contains(reason, problem);
    }

    [Fact]
    public void A_posting_outside_a_transaction_is_reported()
    {
        var problems = Problems("    a  1 USD\n    b\n");

        var problem = Assert.Single(problems);
        Assert.StartsWith("test.journal:1: ", problem);
        Assert.Contains("must belong to a transaction", problem);
    }

    [Fact]
    public void A_transaction_with_a_single_posting_is_reported_at_its_header()
    {
        var problems = Problems("\n2026-01-05 lonely\n    a  0 USD\n");

        Assert.Equal(["test.journal:2: a transaction needs at least two postings"], problems);
    }

    [Fact]
    public void An_unbalanced_transaction_is_reported_at_its_header_with_the_difference()
    {
        var problems = Problems("""
            2026-01-05 off
                a  5 USD
                b  -4 USD
            """);

        var problem = Assert.Single(problems);
        Assert.StartsWith("test.journal:1: ", problem);
        Assert.Contains("1 USD", problem);
    }

    [Theory]
    [InlineData("    a  twelve", "not an amount")]
    [InlineData("    a  1 USD =", "expected an amount after '='")]
    [InlineData("    a  1 USD = abc", "not an amount")]
    [InlineData("    a:  1 USD", "not an account name")]
    [InlineData("    a$b  1 USD", "not an account name")]
    public void A_malformed_posting_is_reported_once_at_its_own_line(string posting, string reason)
    {
        var problems = Problems($"2026-01-05 x\n{posting}\n    b  -1 USD\n");

        var problem = Assert.Single(problems);
        Assert.StartsWith("test.journal:2: ", problem);
        Assert.Contains(reason, problem);
    }

    [Fact]
    public void Every_problem_is_reported_in_one_pass_and_sound_transactions_survive()
    {
        var result = Parse("""
            2026-01-01 good
                a  1 USD
                b

            2026-01-02 bad one
                a  1 USD

            nonsense here

            2026-01-03 bad two
                a  1 USD
                b  -2 USD

            2026-01-04 also good
                a  1 USD
                b
            """);

        Assert.Equal([5, 8, 10], result.Diagnostics.Select(diagnostic => diagnostic.Line));
        Assert.Equal(["good", "also good"], result.Journal.Transactions.Select(transaction => transaction.Description));
    }

    [Fact]
    public void An_unrecognized_line_is_reported_with_what_was_expected()
    {
        var problem = Assert.Single(Problems("frobnicate the ledger\n"));

        Assert.Contains("unrecognized line 'frobnicate the ledger'", problem);
        Assert.Contains("expected", problem);
    }

    [Fact]
    public void A_line_longer_than_the_limit_is_reported_instead_of_read()
    {
        var problem = Assert.Single(Problems("2026-01-05 " + new string('x', 2000) + "\n    a  1 USD\n    b\n"));

        Assert.StartsWith("test.journal:1: ", problem);
        Assert.Contains("longer than", problem);
    }

    [Fact]
    public void A_transaction_with_too_many_postings_is_reported()
    {
        var postings = string.Concat(Enumerable.Repeat("    a  1 USD\n    b  -1 USD\n", 600));

        var problems = Problems("2026-01-05 big\n" + postings);

        Assert.Contains(problems, problem => problem.Contains("at most 1000 postings", StringComparison.Ordinal));
    }

    [Fact]
    public void The_format_directive_accepts_the_supported_format()
    {
        Assert.True(Parse("format 1\n\n2026-01-05 x\n    a  1 USD\n    b\n").IsValid);
    }

    [Fact]
    public void A_journal_of_a_newer_format_is_refused_whole_with_a_message()
    {
        var result = Parse("format 2\n\n2026-01-05 x\n    a  1 USD\n    b\n");

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(1, diagnostic.Line);
        Assert.Contains("format 2", diagnostic.Message);
        Assert.Contains("upgrade", diagnostic.Message);
        Assert.Empty(result.Journal.Transactions);
    }

    [Theory]
    [InlineData("format")]
    [InlineData("format one")]
    [InlineData("format 0")]
    [InlineData("format -1")]
    [InlineData("format 1 2")]
    public void A_malformed_format_directive_is_reported(string directive)
    {
        var problem = Assert.Single(Problems(directive + "\n"));

        Assert.Contains("'format' directive takes", problem);
    }

    [Fact]
    public void The_format_directive_after_a_transaction_is_reported()
    {
        var problem = Assert.Single(Problems("2026-01-05 x\n    a  1 USD\n    b\nformat 1\n"));

        Assert.StartsWith("test.journal:4: ", problem);
        Assert.Contains("before the first transaction", problem);
    }

    [Fact]
    public void Hostile_text_is_reported_and_never_throws()
    {
        string[] pieces =
        [
            "2026-01-05", "2026-13-99", " ", "\t", "\n", "\r\n", "    ", "a:b", ":", "::", "1e20", "-", "+", "=", ";", "#", "*", "!",
            "USD", "99999999999999999999999999999", "-0", "0.0000000000000000000000000001", "format", "\u0000", "\u0007", "é", "💥",
            "assets:cash", "  ", "= 5", "NaN", "Infinity", "1,5", "\\", "\"", "'",
        ];
        var random = new Random(20260105);

        for (var i = 0; i < 2000; i++)
        {
            var text = string.Concat(Enumerable.Range(0, random.Next(1, 40)).Select(_ => pieces[random.Next(pieces.Length)]));

            var result = Parse(text);

            Assert.Equal(result.IsValid, result.Diagnostics.IsEmpty);
        }
    }
}
