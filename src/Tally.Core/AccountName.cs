namespace Tally.Core;

/// <summary>A colon-separated path such as <c>assets:bank:checking</c>. Never default-constructed.</summary>
internal readonly record struct AccountName
{
    private const int MaxLength = 256;

    private AccountName(string value) => Value = value;

    public string Value { get; }

    public static Result<AccountName> Parse(string text)
    {
        if (text.Length == 0 || text.Length > MaxLength)
        {
            return Result<AccountName>.Fail($"an account name is 1 to {MaxLength} characters long");
        }

        foreach (var segment in text.Split(':'))
        {
            if (segment.Length == 0 || !segment.All(IsSegmentCharacter))
            {
                return Result<AccountName>.Fail($"'{(text.Length <= 40 ? text : text[..40] + "...")}' is not an account name; use letters, digits, '-' and '_' in segments separated by ':'");
            }
        }

        return Result<AccountName>.Ok(new AccountName(text));
    }

    /// <summary>True for the account itself and every account beneath it, matched by whole segment.</summary>
    public bool IsWithin(AccountName ancestor) =>
        Value == ancestor.Value || Value.StartsWith(ancestor.Value + ':', StringComparison.Ordinal);

    /// <summary>The first <paramref name="depth"/> segments of the name.</summary>
    public AccountName Truncate(int depth) => new(string.Join(':', Value.Split(':').Take(depth)));

    public override string ToString() => Value;

    private static bool IsSegmentCharacter(char c) => char.IsLetterOrDigit(c) || c is '-' or '_';
}
