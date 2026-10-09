using System.Collections.Immutable;
using System.Globalization;

namespace Tally.Core;

/// <summary>
/// Reads journal text. Never throws: every problem becomes a <see cref="Diagnostic"/> and the sound
/// transactions are still returned. The grammar is in docs/adr/0002 and the README.
/// </summary>
internal static class JournalParser
{
    /// <summary>The newest journal format this reader understands.</summary>
    public const int SupportedFormat = 1;

    private const int MaxLineLength = 1024;
    private const int MaxPostingsPerTransaction = 1000;

    public static ParseResult Parse(string source, string text)
    {
        var run = new Run(source);
        using var reader = new StringReader(text);
        var number = 0;
        while (!run.Refused && reader.ReadLine() is { } line)
        {
            run.Read(line, ++number);
        }

        return run.Finish();
    }

    private sealed class Run(string source)
    {
        private readonly List<Transaction> transactions = [];
        private readonly List<Diagnostic> diagnostics = [];
        private Draft? draft;
        private bool skippingBlock;

        public bool Refused { get; private set; }

        public void Read(string line, int number)
        {
            if (line.Length > MaxLineLength)
            {
                EndTransaction();
                Report(number, $"the line is longer than {MaxLineLength} characters");
                skippingBlock = true;
            }
            else if (string.IsNullOrWhiteSpace(line))
            {
                EndTransaction();
                skippingBlock = false;
            }
            else if (char.IsWhiteSpace(line[0]))
            {
                ReadPosting(line.Trim(), number);
            }
            else
            {
                EndTransaction();
                skippingBlock = false;
                ReadUnindented(line, number);
            }
        }

        public ParseResult Finish()
        {
            if (Refused)
            {
                return new ParseResult(Journal.Empty, [.. diagnostics]);
            }

            EndTransaction();
            var journal = new Journal([.. transactions.OrderBy(transaction => transaction.Date)]);
            diagnostics.AddRange(AssertionChecker.Check(journal, source));
            return new ParseResult(journal, [.. diagnostics.OrderBy(diagnostic => diagnostic.Line)]);
        }

        private void ReadUnindented(string line, int number)
        {
            if (line[0] is ';' or '#')
            {
                return;
            }

            if (char.IsAsciiDigit(line[0]))
            {
                ReadHeader(line, number);
                return;
            }

            var words = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words[0] == "format")
            {
                ReadFormat(words, number);
                return;
            }

            Report(number, $"unrecognized line '{Excerpt(line)}'; expected a transaction date, a 'format' directive or a comment");
        }

        private void ReadFormat(string[] words, int number)
        {
            if (transactions.Count > 0 || draft is not null)
            {
                Report(number, "the 'format' directive must come before the first transaction");
            }
            else if (words.Length != 2 || !int.TryParse(words[1], NumberStyles.None, CultureInfo.InvariantCulture, out var format) || format < 1)
            {
                Report(number, "the 'format' directive takes one positive whole number, such as 'format 1'");
            }
            else if (format > SupportedFormat)
            {
                Report(number, $"the journal is format {format}, newer than the format {SupportedFormat} this version reads; upgrade tally");
                Refused = true;
            }
        }

        private void ReadHeader(string line, int number)
        {
            var text = StripComment(line);
            if (text.Length < 10
                || !DateOnly.TryParseExact(text[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                || (text.Length > 10 && !char.IsWhiteSpace(text[10])))
            {
                Report(number, $"'{Excerpt(text)}' does not start with a date; expected YYYY-MM-DD");
                skippingBlock = true;
                return;
            }

            var rest = text[10..].Trim();
            var status = TransactionStatus.Unmarked;
            if (rest.Length > 0 && rest[0] is '*' or '!' && (rest.Length == 1 || char.IsWhiteSpace(rest[1])))
            {
                status = rest[0] == '*' ? TransactionStatus.Cleared : TransactionStatus.Pending;
                rest = rest[1..].Trim();
            }

            draft = new Draft(date, status, rest, number);
        }

        private void ReadPosting(string line, int number)
        {
            if (line[0] == ';' || skippingBlock)
            {
                return;
            }

            if (draft is null)
            {
                Report(number, "a posting must belong to a transaction; start one with a date line");
                skippingBlock = true;
                return;
            }

            if (draft.Postings.Count >= MaxPostingsPerTransaction)
            {
                Report(number, $"a transaction has at most {MaxPostingsPerTransaction} postings");
                draft.Invalid = true;
                return;
            }

            var text = StripComment(line);
            var split = text.IndexOfAny([' ', '\t']);
            var accountText = split < 0 ? text : text[..split];
            var amountText = split < 0 ? "" : text[split..].Trim();

            var account = AccountName.Parse(accountText);
            if (!account.IsOk)
            {
                Report(number, account.Error);
                draft.Invalid = true;
                return;
            }

            var equals = amountText.IndexOf('=', StringComparison.Ordinal);
            var writtenText = equals < 0 ? amountText : amountText[..equals].Trim();
            var assertedText = equals < 0 ? null : amountText[(equals + 1)..].Trim();

            Amount? amount = null;
            Amount? assertion = null;
            var sound = true;
            if (writtenText.Length > 0)
            {
                amount = ReadAmount(writtenText, number);
                sound = amount is not null;
            }

            if (assertedText is not null)
            {
                assertion = ReadAmount(assertedText, number);
                sound &= assertion is not null;
            }

            if (sound)
            {
                draft.Postings.Add(new DraftPosting(account.Value, amount, assertion, number));
            }
            else
            {
                draft.Invalid = true;
            }
        }

        private Amount? ReadAmount(string text, int number)
        {
            var amount = text.Length == 0 ? Result<Amount>.Fail("expected an amount after '='") : Amount.Parse(text);
            if (amount.IsOk)
            {
                return amount.Value;
            }

            Report(number, amount.Error);
            return null;
        }

        private void EndTransaction()
        {
            if (draft is not { } current)
            {
                return;
            }

            draft = null;
            if (current.Invalid)
            {
                return;
            }

            if (current.Postings.Count < 2)
            {
                Report(current.Line, "a transaction needs at least two postings");
                return;
            }

            var postings = TransactionBalancer.Resolve(current.Postings);
            if (!postings.IsOk)
            {
                Report(current.Line, postings.Error);
                return;
            }

            transactions.Add(new Transaction(current.Date, current.Status, current.Description, postings.Value, current.Line));
        }

        private void Report(int number, string message) => diagnostics.Add(new Diagnostic(source, number, message));

        private static string StripComment(string line)
        {
            var comment = line.IndexOf(';', StringComparison.Ordinal);
            return (comment < 0 ? line : line[..comment]).Trim();
        }

        private static string Excerpt(string text) => text.Length <= 40 ? text : text[..40] + "...";
    }

    private sealed class Draft(DateOnly date, TransactionStatus status, string description, int line)
    {
        public DateOnly Date { get; } = date;

        public TransactionStatus Status { get; } = status;

        public string Description { get; } = description;

        public int Line { get; } = line;

        public List<DraftPosting> Postings { get; } = [];

        public bool Invalid { get; set; }
    }
}
