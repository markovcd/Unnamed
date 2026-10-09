using System.Collections.Immutable;

namespace Tally.Core;

/// <summary>What each account holds after the filtered postings, and the sum over all accounts.</summary>
internal sealed record BalanceReport(ImmutableArray<BalanceRow> Rows, Balance Total)
{
    /// <param name="depth">When given, accounts deeper than this many segments are rolled up into their ancestor.</param>
    public static BalanceReport Build(Journal journal, PostingFilter filter, int? depth = null)
    {
        var balances = new SortedDictionary<string, (AccountName Account, Balance Balance)>(StringComparer.Ordinal);
        var total = Balance.Empty;

        foreach (var transaction in journal.Transactions)
        {
            foreach (var posting in transaction.Postings.Where(posting => filter.Matches(transaction, posting)))
            {
                var account = depth is { } limit ? posting.Account.Truncate(limit) : posting.Account;
                var held = balances.TryGetValue(account.Value, out var entry) ? entry.Balance : Balance.Empty;
                balances[account.Value] = (account, held.Plus(posting.Amount));
                total = total.Plus(posting.Amount);
            }
        }

        return new BalanceReport([.. balances.Values.Select(entry => new BalanceRow(entry.Account, entry.Balance))], total);
    }
}
