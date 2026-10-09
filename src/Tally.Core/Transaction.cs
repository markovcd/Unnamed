using System.Collections.Immutable;

namespace Tally.Core;

/// <summary>A dated movement between accounts whose postings sum to zero in every commodity.</summary>
internal sealed record Transaction(
    DateOnly Date,
    TransactionStatus Status,
    string Description,
    ImmutableArray<Posting> Postings,
    int Line);
