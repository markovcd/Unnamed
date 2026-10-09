using Tally.Core;

namespace Tally.Cli.Tests;

public sealed class CommandLineParserTests
{
    private static CommandLine Read(params string[] args) => CommandLineParser.Parse(args).Value;

    private static string Refusal(params string[] args)
    {
        var result = CommandLineParser.Parse(args);
        Assert.False(result.IsOk);
        return result.Error;
    }

    [Fact]
    public void A_command_and_a_journal_are_enough()
    {
        var line = Read("balance", "my.journal");

        Assert.Equal("balance", line.Command.Name);
        Assert.Equal("my.journal", line.Journal);
        Assert.False(line.Json);
    }

    [Fact]
    public void A_lone_dash_names_standard_input()
    {
        Assert.Equal("-", Read("check", "-").Journal);
    }

    [Fact]
    public void Options_may_come_before_or_after_the_journal_and_take_their_value_either_way()
    {
        var line = Read("balance", "--account=assets", "j", "--from", "2026-01-01", "--to=2026-02-01", "--depth", "2", "--json");

        Assert.Equal("assets", line.Account?.Value);
        Assert.Equal(new DateOnly(2026, 1, 1), line.From);
        Assert.Equal(new DateOnly(2026, 2, 1), line.To);
        Assert.Equal(2, line.Depth);
        Assert.True(line.Json);
    }

    [Fact]
    public void Every_registered_command_parses_with_just_a_journal()
    {
        foreach (var command in Commands.All)
        {
            Assert.Equal(command.Name, Read(command.Name, "j").Command.Name);
        }
    }

    [Theory]
    [InlineData("no command given")]
    public void An_empty_command_line_is_refused(string reason)
    {
        Assert.Contains(reason, Refusal());
    }

    [Fact]
    public void An_unknown_command_is_named()
    {
        Assert.Contains("unknown command 'frob'", Refusal("frob", "j"));
    }

    [Fact]
    public void A_missing_journal_is_refused_and_standard_input_is_suggested()
    {
        Assert.Contains("use - for standard input", Refusal("balance"));
    }

    [Fact]
    public void A_second_journal_is_refused()
    {
        Assert.Contains("one journal", Refusal("balance", "a", "b"));
    }

    [Fact]
    public void An_option_the_command_does_not_take_is_refused_by_name()
    {
        Assert.Contains("'accounts' does not take the option '--depth'", Refusal("accounts", "j", "--depth", "1"));
        Assert.Contains("'balance' does not take the option '--nope'", Refusal("balance", "j", "--nope"));
        Assert.Contains("'balance' does not take the option '-x'", Refusal("balance", "j", "-x"));
    }

    [Fact]
    public void An_option_missing_its_value_is_refused()
    {
        Assert.Contains("'--account' needs a NAME", Refusal("balance", "j", "--account"));
    }

    [Fact]
    public void A_flag_given_a_value_is_refused()
    {
        Assert.Contains("takes no value", Refusal("balance", "j", "--json=yes"));
    }

    [Theory]
    [InlineData("--from", "2026-1-5", "YYYY-MM-DD")]
    [InlineData("--to", "tomorrow", "YYYY-MM-DD")]
    [InlineData("--depth", "0", "from 1 to 64")]
    [InlineData("--depth", "65", "from 1 to 64")]
    [InlineData("--depth", "-1", "from 1 to 64")]
    [InlineData("--depth", "two", "from 1 to 64")]
    [InlineData("--account", "a b", "not an account name")]
    public void A_bad_option_value_is_refused_with_what_was_expected(string option, string value, string reason)
    {
        Assert.Contains(reason, Refusal("balance", "j", option, value));
    }

    [Fact]
    public void A_range_that_ends_before_it_starts_is_refused()
    {
        Assert.Contains("is after", Refusal("balance", "j", "--from", "2026-02-01", "--to", "2026-01-01"));
    }
}
