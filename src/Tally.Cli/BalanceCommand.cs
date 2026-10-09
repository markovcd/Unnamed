using Tally.Core;

namespace Tally.Cli;

internal static class BalanceCommand
{
    public static int Run(CommandLine options, ParseResult loaded, TextWriter output)
    {
        var report = BalanceReport.Build(loaded.Journal, options.Filter, options.Depth);

        if (options.Json)
        {
            output.WriteLine(JsonOutput.Write(writer =>
            {
                writer.WriteStartObject();
                writer.WriteStartArray("accounts");
                foreach (var row in report.Rows)
                {
                    writer.WriteStartObject();
                    writer.WriteString("account", row.Account.Value);
                    JsonOutput.Balance(writer, "balance", row.Balance);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                JsonOutput.Balance(writer, "total", report.Total);
                writer.WriteEndObject();
            }));
            return ExitCodes.Ok;
        }

        if (report.Rows.IsEmpty)
        {
            return ExitCodes.Ok;
        }

        var rows = report.Rows.Select(row => (Lines: AmountLines(row.Balance), row.Account)).ToList();
        var total = AmountLines(report.Total);
        var width = rows.SelectMany(row => row.Lines).Concat(total).Max(line => line.Length);

        foreach (var (lines, account) in rows)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                output.WriteLine($"{lines[i].PadLeft(width)}  {(i == 0 ? account.Value : "")}".TrimEnd());
            }
        }

        output.WriteLine(new string('-', width));
        foreach (var line in total)
        {
            output.WriteLine(line.PadLeft(width));
        }

        return ExitCodes.Ok;
    }

    private static List<string> AmountLines(Balance balance) =>
        balance.IsZero ? ["0"] : [.. balance.Amounts.Select(amount => amount.ToString())];
}
