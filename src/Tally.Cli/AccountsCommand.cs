using Tally.Core;

namespace Tally.Cli;

internal static class AccountsCommand
{
    public static int Run(CommandLine options, ParseResult loaded, TextWriter output)
    {
        var accounts = loaded.Journal.Accounts();

        if (options.Json)
        {
            output.WriteLine(JsonOutput.Write(writer =>
            {
                writer.WriteStartObject();
                writer.WriteStartArray("accounts");
                foreach (var account in accounts)
                {
                    writer.WriteStringValue(account.Value);
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }));
        }
        else
        {
            foreach (var account in accounts)
            {
                output.WriteLine(account.Value);
            }
        }

        return ExitCodes.Ok;
    }
}
