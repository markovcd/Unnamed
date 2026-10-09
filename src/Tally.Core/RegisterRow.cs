namespace Tally.Core;

/// <summary>One posting with the running balance of all filtered postings up to and including it.</summary>
internal sealed record RegisterRow(DateOnly Date, string Description, AccountName Account, Amount Amount, Balance Running);
