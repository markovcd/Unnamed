namespace Tally.Cli;

/// <param name="ValueName">The placeholder for the option's value in help text; null for a flag.</param>
internal sealed record OptionSpec(string Name, string? ValueName, string Summary);
