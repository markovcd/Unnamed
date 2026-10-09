using System.Collections.Immutable;

namespace Tally.Cli;

/// <summary>Every option any command accepts; <see cref="Command.Options"/> picks from these.</summary>
internal static class Options
{
    public const string Account = "--account";
    public const string From = "--from";
    public const string To = "--to";
    public const string Depth = "--depth";
    public const string Json = "--json";

    public static ImmutableArray<OptionSpec> All { get; } =
    [
        new(Account, "NAME", "Only postings to NAME and the accounts beneath it."),
        new(From, "DATE", "Only transactions on or after DATE (YYYY-MM-DD)."),
        new(To, "DATE", "Only transactions on or before DATE (YYYY-MM-DD)."),
        new(Depth, "N", "Roll accounts deeper than N segments up into their ancestor."),
        new(Json, null, "Write the answer as one line of JSON."),
    ];
}
