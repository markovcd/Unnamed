using System.Globalization;
using System.Text;

namespace Tally.Core;

/// <summary>Writes a journal in canonical form: every amount explicit, amounts aligned, text that parses back to the same journal.</summary>
internal static class JournalPrinter
{
    public static string Print(Journal journal)
    {
        var text = new StringBuilder();
        text.Append("format ").Append(JournalParser.SupportedFormat.ToString(CultureInfo.InvariantCulture)).Append('\n');

        foreach (var transaction in journal.Transactions)
        {
            text.Append('\n').Append(Header(transaction)).Append('\n');

            var accountWidth = transaction.Postings.Max(posting => posting.Account.Value.Length);
            var amounts = transaction.Postings.Select(posting => posting.Amount.ToString()).ToList();
            var amountWidth = amounts.Max(amount => amount.Length);

            for (var i = 0; i < transaction.Postings.Length; i++)
            {
                var posting = transaction.Postings[i];
                text.Append("    ")
                    .Append(posting.Account.Value.PadRight(accountWidth + 2))
                    .Append(amounts[i].PadLeft(amountWidth));
                if (posting.Assertion is { } assertion)
                {
                    text.Append(" = ").Append(assertion);
                }

                text.Append('\n');
            }
        }

        return text.ToString();
    }

    private static string Header(Transaction transaction)
    {
        var date = transaction.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var status = transaction.Status switch
        {
            TransactionStatus.Cleared => " *",
            TransactionStatus.Pending => " !",
            _ => "",
        };
        var description = transaction.Description.Length == 0 ? "" : " " + transaction.Description;
        return date + status + description;
    }
}
