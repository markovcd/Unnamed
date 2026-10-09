using Tally.Core;

namespace Tally.Cli;

internal static class PrintCommand
{
    public static int Run(CommandLine options, ParseResult loaded, TextWriter output)
    {
        output.Write(JournalPrinter.Print(loaded.Journal));
        return ExitCodes.Ok;
    }
}
