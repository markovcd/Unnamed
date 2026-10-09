namespace Tally.Core;

/// <summary>Selects postings by account (the account and everything beneath it) and by date, both ends inclusive.</summary>
internal sealed record PostingFilter(AccountName? Account = null, DateOnly? From = null, DateOnly? To = null)
{
    public static PostingFilter None { get; } = new();

    public bool Matches(Transaction transaction, Posting posting) =>
        (Account is not { } account || posting.Account.IsWithin(account))
        && (From is not { } from || transaction.Date >= from)
        && (To is not { } to || transaction.Date <= to);
}
