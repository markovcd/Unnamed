using System.Collections.Immutable;

namespace Tally.Core;

/// <summary>Fills in the one omitted amount of a transaction and proves each commodity sums to zero.</summary>
internal static class TransactionBalancer
{
    public static Result<ImmutableArray<Posting>> Resolve(IReadOnlyList<DraftPosting> drafts)
    {
        var sum = drafts.Where(draft => draft.Amount is not null)
            .Aggregate(Balance.Empty, (balance, draft) => balance.Plus(draft.Amount!.Value));
        var omitted = drafts.Count(draft => draft.Amount is null);

        if (omitted > 1)
        {
            return Result<ImmutableArray<Posting>>.Fail("only one posting may omit its amount");
        }

        if (omitted == 0 && !sum.IsZero)
        {
            return Result<ImmutableArray<Posting>>.Fail($"the transaction does not balance; the postings are off by {sum}");
        }

        var postings = ImmutableArray.CreateBuilder<Posting>(drafts.Count);
        foreach (var draft in drafts)
        {
            if (draft.Amount is { } amount)
            {
                postings.Add(new Posting(draft.Account, amount, draft.Assertion, draft.Line));
                continue;
            }

            // The assertion rides on the last posting standing in for the omitted one, so every commodity it fills in is already applied when it is checked.
            var inferred = sum.Negated().Amounts.DefaultIfEmpty(new Amount(0, "")).ToList();
            for (var i = 0; i < inferred.Count; i++)
            {
                var assertion = i == inferred.Count - 1 ? draft.Assertion : null;
                postings.Add(new Posting(draft.Account, inferred[i], assertion, draft.Line));
            }
        }

        return Result<ImmutableArray<Posting>>.Ok(postings.ToImmutable());
    }
}
