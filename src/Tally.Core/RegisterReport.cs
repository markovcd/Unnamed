using System.Collections.Immutable;

namespace Tally.Core;

internal sealed record RegisterReport(ImmutableArray<RegisterRow> Rows)
{
    public static RegisterReport Build(Journal journal, PostingFilter filter)
    {
        var rows = ImmutableArray.CreateBuilder<RegisterRow>();
        var running = Balance.Empty;

        foreach (var transaction in journal.Transactions)
        {
            foreach (var posting in transaction.Postings.Where(posting => filter.Matches(transaction, posting)))
            {
                running = running.Plus(posting.Amount);
                rows.Add(new RegisterRow(transaction.Date, transaction.Description, posting.Account, posting.Amount, running));
            }
        }

        return new RegisterReport(rows.ToImmutable());
    }
}
