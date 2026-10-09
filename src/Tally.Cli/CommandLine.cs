using Tally.Core;

namespace Tally.Cli;

/// <summary>A validated command line. <paramref name="Journal"/> is a path, or <c>-</c> for standard input.</summary>
internal sealed record CommandLine(
    Command Command,
    string Journal,
    AccountName? Account,
    DateOnly? From,
    DateOnly? To,
    int? Depth,
    bool Json)
{
    public PostingFilter Filter => new(Account, From, To);
}
