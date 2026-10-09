using System.Collections.Immutable;
using Tally.Core;

namespace Tally.Cli;

/// <summary>A command is a function of its arguments, the parsed journal and an output writer, returning an exit code.</summary>
/// <param name="Options">The options the command accepts, by name.</param>
/// <param name="ToleratesInvalid">True when the command reports on a journal with problems instead of refusing it.</param>
internal sealed record Command(
    string Name,
    string Summary,
    ImmutableArray<string> Options,
    bool ToleratesInvalid,
    Func<CommandLine, ParseResult, TextWriter, int> Run);
