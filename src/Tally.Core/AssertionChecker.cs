using System.Collections.Immutable;

namespace Tally.Core;

/// <summary>Replays a journal in order and compares each balance assertion with the account's running balance.</summary>
internal static class AssertionChecker
{
    public static ImmutableArray<Diagnostic> Check(Journal journal, string source)
    {
        var running = new Dictionary<(AccountName Account, string Commodity), decimal>();
        var failures = ImmutableArray.CreateBuilder<Diagnostic>();

        foreach (var posting in journal.Transactions.SelectMany(transaction => transaction.Postings))
        {
            var key = (posting.Account, posting.Amount.Commodity);
            running[key] = running.GetValueOrDefault(key) + posting.Amount.Quantity;

            if (posting.Assertion is not { } expected)
            {
                continue;
            }

            var actual = running.GetValueOrDefault((posting.Account, expected.Commodity));
            if (actual != expected.Quantity)
            {
                failures.Add(new Diagnostic(
                    source,
                    posting.Line,
                    $"balance assertion failed for {posting.Account}: expected {expected}, actual {new Amount(actual, expected.Commodity)}"));
            }
        }

        return failures.ToImmutable();
    }
}
