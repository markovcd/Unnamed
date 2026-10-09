namespace Tally.Cli.Tests;

/// <summary>The outcome of running the application in-process.</summary>
internal sealed record Run(int Exit, string Output, string Error)
{
    public static Run Of(string stdin, params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exit = Application.Run(args, new StringReader(stdin), output, error);
        return new Run(exit, output.ToString(), error.ToString());
    }
}
