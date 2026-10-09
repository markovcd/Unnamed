namespace Tally.Core;

/// <summary>One account's side of a transaction. <paramref name="Assertion"/> is the account's expected balance in that commodity right after this posting.</summary>
internal sealed record Posting(AccountName Account, Amount Amount, Amount? Assertion, int Line);
