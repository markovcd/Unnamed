namespace Tally.Cli.Tests;

public sealed class DocumentationTests
{
    private static string Readme { get; } = File.ReadAllText(Path.Combine(RepositoryRoot.Path, "README.md"));

    public static TheoryData<string> CommandNames() => [.. Commands.All.Select(command => command.Name)];

    public static TheoryData<string> OptionNames() => [.. Options.All.Select(option => option.Name)];

    [Theory]
    [MemberData(nameof(CommandNames))]
    public void The_readme_documents_every_command(string name)
    {
        Assert.Contains($"tally {name}", Readme);
    }

    [Theory]
    [MemberData(nameof(OptionNames))]
    public void The_readme_documents_every_option(string name)
    {
        Assert.Contains($"`{name}", Readme);
    }

    [Fact]
    public void The_usage_text_lists_every_command_and_option()
    {
        var usage = Usage.Text();

        Assert.All(Commands.All, command => Assert.Contains(command.Name, usage));
        Assert.All(Options.All, option => Assert.Contains(option.Name, usage));
    }

    public static TheoryData<string> Examples() =>
        [.. Directory.EnumerateFiles(Path.Combine(RepositoryRoot.Path, "examples"), "*.journal").Select(path => Path.GetRelativePath(RepositoryRoot.Path, path))];

    [Theory]
    [MemberData(nameof(Examples))]
    public void Every_shipped_example_journal_checks_clean(string relativePath)
    {
        var run = Run.Of("", "check", Path.Combine(RepositoryRoot.Path, relativePath));

        Assert.Equal((0, ""), (run.Exit, run.Error));
    }
}
