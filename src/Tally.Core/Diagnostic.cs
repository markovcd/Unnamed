namespace Tally.Core;

/// <summary>A problem found in a journal. Line 0 means the problem is with the source as a whole.</summary>
internal readonly record struct Diagnostic(string Source, int Line, string Message)
{
    public override string ToString() => Line == 0 ? $"{Source}: {Message}" : $"{Source}:{Line}: {Message}";
}
