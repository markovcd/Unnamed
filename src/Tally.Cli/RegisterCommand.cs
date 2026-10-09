using Tally.Core;

namespace Tally.Cli;

internal static class RegisterCommand
{
    public static int Run(CommandLine options, ParseResult loaded, TextWriter output)
    {
        var report = RegisterReport.Build(loaded.Journal, options.Filter);

        if (options.Json)
        {
            output.WriteLine(JsonOutput.Write(writer =>
            {
                writer.WriteStartObject();
                writer.WriteStartArray("postings");
                foreach (var row in report.Rows)
                {
                    writer.WriteStartObject();
                    writer.WriteString("date", JsonOutput.Date(row.Date));
                    writer.WriteString("description", row.Description);
                    writer.WriteString("account", row.Account.Value);
                    writer.WritePropertyName("amount");
                    JsonOutput.Amount(writer, row.Amount);
                    JsonOutput.Balance(writer, "balance", row.Running);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }));
            return ExitCodes.Ok;
        }

        if (report.Rows.IsEmpty)
        {
            return ExitCodes.Ok;
        }

        var descriptionWidth = report.Rows.Max(row => row.Description.Length);
        var accountWidth = report.Rows.Max(row => row.Account.Value.Length);
        var amountWidth = report.Rows.Max(row => row.Amount.ToString().Length);

        foreach (var row in report.Rows)
        {
            output.WriteLine(string.Join(
                "  ",
                JsonOutput.Date(row.Date),
                row.Description.PadRight(descriptionWidth),
                row.Account.Value.PadRight(accountWidth),
                row.Amount.ToString().PadLeft(amountWidth),
                row.Running.ToString()));
        }

        return ExitCodes.Ok;
    }
}
