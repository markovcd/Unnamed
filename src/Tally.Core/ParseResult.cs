using System.Collections.Immutable;

namespace Tally.Core;

/// <summary>The transactions that were sound and every problem found. A journal with problems is not to be reported on.</summary>
internal sealed record ParseResult(Journal Journal, ImmutableArray<Diagnostic> Diagnostics)
{
    public bool IsValid => Diagnostics.IsEmpty;
}
