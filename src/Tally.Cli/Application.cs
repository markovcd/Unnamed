namespace Tally.Cli;

internal static class Application
{
    /// <summary>Runs one command. Answers go to <paramref name="output"/>; diagnostics and usage errors go to <paramref name="error"/>.</summary>
    public static int Run(string[] args, TextReader input, TextWriter output, TextWriter error)
    {
        if (args is ["help" or "--help" or "-h"])
        {
            output.Write(Usage.Text());
            return ExitCodes.Ok;
        }

        var options = CommandLineParser.Parse(args);
        if (!options.IsOk)
        {
            error.WriteLine($"tally: {options.Error}");
            error.WriteLine("Run 'tally help' for usage.");
            return ExitCodes.Usage;
        }

        var loaded = JournalLoader.Load(options.Value.Journal, input);
        if (!loaded.IsValid && !options.Value.Command.ToleratesInvalid)
        {
            WriteDiagnostics(loaded, error);
            return ExitCodes.InvalidJournal;
        }

        var exit = options.Value.Command.Run(options.Value, loaded, output);
        if (!options.Value.Json)
        {
            WriteDiagnostics(loaded, error);
        }

        return exit;
    }

    private static void WriteDiagnostics(Core.ParseResult loaded, TextWriter error)
    {
        foreach (var diagnostic in loaded.Diagnostics)
        {
            error.WriteLine(diagnostic);
        }
    }
}
