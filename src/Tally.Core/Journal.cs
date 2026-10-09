using System.Collections.Immutable;

namespace Tally.Core;

/// <summary>Transactions in date order; transactions on one date keep their order in the source.</summary>
internal sealed class Journal(ImmutableArray<Transaction> transactions)
{
    public static Journal Empty { get; } = new([]);

    public ImmutableArray<Transaction> Transactions { get; } = transactions;

    /// <summary>Every account that has a posting, ordered by name.</summary>
    public IReadOnlyList<AccountName> Accounts() => Transactions
        .SelectMany(transaction => transaction.Postings)
        .Select(posting => posting.Account)
        .Distinct()
        .OrderBy(account => account.Value, StringComparer.Ordinal)
        .ToList();
}
