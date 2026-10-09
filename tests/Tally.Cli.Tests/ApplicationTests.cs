namespace Tally.Cli.Tests;

public sealed class ApplicationTests : IDisposable
{
    private const string Household = """
        2026-01-01 * Opening
            assets:cash   100.00 USD
            equity

        2026-01-10 * Groceries
            expenses:food   40.00 USD
            assets:cash

        2026-02-03 Gift
            assets:cash   10.00 EUR
            income:gifts
        """;

    private readonly string folder = Directory.CreateTempSubdirectory("tally-tests-").FullName;

    public void Dispose() => Directory.Delete(folder, recursive: true);

    private string Journal(string text)
    {
        var path = Path.Combine(folder, "test.journal");
        File.WriteAllText(path, text);
        return path;
    }

    [Fact]
    public void Check_says_ok_with_counts_and_exits_zero_for_a_sound_journal()
    {
        var run = Run.Of("", "check", Journal(Household));

        Assert.Equal((0, "ok: 3 transactions, 4 accounts\n", ""), (run.Exit, run.Output, run.Error));
    }

    [Fact]
    public void Check_lists_every_problem_with_its_line_and_exits_one()
    {
        var run = Run.Of("2026-01-01 a\n  x 1 USD\n\nnonsense\n", "check", "-");

        Assert.Equal(1, run.Exit);
        Assert.Equal("", run.Output);
        Assert.Equal(
            "<stdin>:1: a transaction needs at least two postings\n<stdin>:4: unrecognized line 'nonsense'; expected a transaction date, a 'format' directive or a comment\n",
            run.Error.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Check_as_json_puts_the_problems_on_standard_output_and_still_exits_one()
    {
        var run = Run.Of("nonsense\n", "check", "--json", "-");

        Assert.Equal(1, run.Exit);
        Assert.Equal("", run.Error);
        Assert.StartsWith("{\"valid\":false,\"transactions\":0,\"accounts\":0,\"diagnostics\":[{\"source\":\"<stdin>\",\"line\":1,", run.Output);
    }

    [Fact]
    public void Balance_shows_each_account_and_a_total_aligned_on_the_right()
    {
        var run = Run.Of("", "balance", Journal(Household));

        Assert.Equal(0, run.Exit);
        Assert.Equal(
            """
              10.00 EUR  assets:cash
              60.00 USD
            -100.00 USD  equity
              40.00 USD  expenses:food
             -10.00 EUR  income:gifts
            -----------
                      0

            """,
            run.Output.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Balance_honors_account_depth_and_date_options()
    {
        var run = Run.Of("", "balance", "--account", "expenses", "--to", "2026-01-31", Journal(Household));

        Assert.Equal(
            "40.00 USD  expenses:food\n---------\n40.00 USD\n",
            run.Output.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Balance_as_json_writes_quantities_as_strings_on_one_line()
    {
        var run = Run.Of("", "balance", "--json", "--account", "expenses", Journal(Household));

        Assert.Equal(
            "{\"accounts\":[{\"account\":\"expenses:food\",\"balance\":[{\"quantity\":\"40.00\",\"commodity\":\"USD\"}]}],\"total\":[{\"quantity\":\"40.00\",\"commodity\":\"USD\"}]}\n",
            run.Output.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Register_lists_postings_with_a_running_balance()
    {
        var run = Run.Of("", "register", "--account", "assets:cash", Journal(Household));

        Assert.Equal(
            """
            2026-01-01  Opening    assets:cash  100.00 USD  100.00 USD
            2026-01-10  Groceries  assets:cash  -40.00 USD  60.00 USD
            2026-02-03  Gift       assets:cash   10.00 EUR  10.00 EUR, 60.00 USD

            """,
            run.Output.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Register_as_json_carries_the_running_balance_of_each_posting()
    {
        var run = Run.Of("", "register", "--json", "--from", "2026-01-10", "--to", "2026-01-10", "--account", "expenses", Journal(Household));

        Assert.Equal(
            "{\"postings\":[{\"date\":\"2026-01-10\",\"description\":\"Groceries\",\"account\":\"expenses:food\",\"amount\":{\"quantity\":\"40.00\",\"commodity\":\"USD\"},\"balance\":[{\"quantity\":\"40.00\",\"commodity\":\"USD\"}]}]}\n",
            run.Output.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Accounts_lists_one_name_per_line_in_order()
    {
        var run = Run.Of("", "accounts", Journal(Household));

        Assert.Equal(
            "assets:cash\nequity\nexpenses:food\nincome:gifts\n",
            run.Output.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Accounts_as_json_is_a_list_of_names()
    {
        var run = Run.Of(Household, "accounts", "--json", "-");

        Assert.Equal("{\"accounts\":[\"assets:cash\",\"equity\",\"expenses:food\",\"income:gifts\"]}\n", run.Output.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void Print_writes_canonical_text_that_check_accepts()
    {
        var printed = Run.Of(Household, "print", "-").Output;

        Assert.StartsWith("format 1\n", printed.Replace("\r\n", "\n", StringComparison.Ordinal));
        Assert.Equal(0, Run.Of(printed, "check", "-").Exit);
    }

    [Theory]
    [InlineData("balance")]
    [InlineData("register")]
    [InlineData("accounts")]
    [InlineData("print")]
    public void A_report_on_a_journal_with_problems_prints_the_problems_and_no_report(string command)
    {
        var run = Run.Of("2026-01-01 off\n  a 5 USD\n  b -4 USD\n", command, "-");

        Assert.Equal(1, run.Exit);
        Assert.Equal("", run.Output);
        Assert.Contains("<stdin>:1: the transaction does not balance", run.Error);
    }

    [Fact]
    public void A_missing_file_is_a_journal_problem_not_a_crash()
    {
        var run = Run.Of("", "balance", Path.Combine(folder, "absent.journal"));

        Assert.Equal(1, run.Exit);
        Assert.Contains("absent.journal: cannot read the journal: no such file", run.Error);
    }

    [Fact]
    public void A_directory_given_as_the_journal_is_a_journal_problem_not_a_crash()
    {
        var run = Run.Of("", "balance", folder);

        Assert.Equal(1, run.Exit);
        Assert.Contains("cannot read the journal", run.Error);
    }

    [Fact]
    public void A_journal_past_the_size_limit_is_refused_before_it_is_read_into_memory()
    {
        var path = Path.Combine(folder, "big.journal");
        using (var file = File.Create(path))
        {
            file.SetLength(JournalLoader.MaxCharacters + 1L);
        }

        var run = Run.Of("", "check", path);

        Assert.Equal(1, run.Exit);
        Assert.Contains("larger than 16 MiB", run.Error);
    }

    [Fact]
    public void Standard_input_past_the_size_limit_is_refused()
    {
        var run = Run.Of(new string(';', JournalLoader.MaxCharacters + 1), "check", "-");

        Assert.Equal(1, run.Exit);
        Assert.Contains("larger than 16 million characters", run.Error);
    }

    [Fact]
    public void A_bad_command_line_exits_two_with_a_pointer_to_help_and_nothing_on_standard_output()
    {
        var run = Run.Of("", "balance");

        Assert.Equal(2, run.Exit);
        Assert.Equal("", run.Output);
        Assert.Contains("Run 'tally help' for usage.", run.Error);
    }

    [Theory]
    [InlineData("help")]
    [InlineData("--help")]
    [InlineData("-h")]
    public void Help_prints_usage_to_standard_output_and_exits_zero(string argument)
    {
        var run = Run.Of("", argument);

        Assert.Equal(0, run.Exit);
        Assert.StartsWith("tally - plain-text double-entry bookkeeping", run.Output);
        Assert.Equal("", run.Error);
    }
}
