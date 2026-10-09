namespace Tally.Core;

/// <summary>A posting as written; a null amount is left for the transaction to infer.</summary>
internal sealed record DraftPosting(AccountName Account, Amount? Amount, Amount? Assertion, int Line);
