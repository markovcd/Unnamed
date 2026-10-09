using Tally.Core;

namespace Tally.Cli;

internal static class CheckCommand
{
    public static int Run(CommandLine options, ParseResult loaded, TextWriter output)
    {
        var accounts = loaded.Journal.Accounts().Count;
        var transactions = loaded.Journal.Transactions.Length;

        if (options.Json)
        {
            output.WriteLine(JsonOutput.Write(writer =>
            {
                writer.WriteStartObject();
                writer.WriteBoolean("valid", loaded.IsValid);
                writer.WriteNumber("transactions", transactions);
                writer.WriteNumber("accounts", accounts);
                writer.WriteStartArray("diagnostics");
                foreach (var diagnostic in loaded.Diagnostics)
                {
                    writer.WriteStartObject();
                    writer.WriteString("source", diagnostic.Source);
                    writer.WriteNumber("line", diagnostic.Line);
                    writer.WriteString("message", diagnostic.Message);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }));
        }
        else if (loaded.IsValid)
        {
            output.WriteLine($"ok: {transactions} transactions, {accounts} accounts");
        }

        return loaded.IsValid ? ExitCodes.Ok : ExitCodes.InvalidJournal;
    }
}
