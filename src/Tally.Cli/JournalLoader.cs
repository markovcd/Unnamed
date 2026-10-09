using System.Collections.Immutable;
using System.Text;
using Tally.Core;

namespace Tally.Cli;

/// <summary>Reads a journal from a file or standard input; a read failure is a diagnostic, not an exception.</summary>
internal static class JournalLoader
{
    public const int MaxCharacters = 16 * 1024 * 1024;

    private const string StdinName = "<stdin>";

    public static ParseResult Load(string path, TextReader stdin)
    {
        var source = path == "-" ? StdinName : path;
        var text = path == "-" ? ReadStdin(stdin) : ReadFile(path);
        return text.IsOk
            ? JournalParser.Parse(source, text.Value)
            : new ParseResult(Journal.Empty, [new Diagnostic(source, 0, text.Error)]);
    }

    private static Result<string> ReadFile(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                return Result<string>.Fail("cannot read the journal: no such file");
            }

            // The length is in bytes, so a file this size can never decode to more characters than the cap.
            return info.Length > MaxCharacters
                ? Result<string>.Fail($"the journal is larger than {MaxCharacters / (1024 * 1024)} MiB")
                : Result<string>.Ok(File.ReadAllText(path));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return Result<string>.Fail($"cannot read the journal: {e.Message}");
        }
    }

    private static Result<string> ReadStdin(TextReader stdin)
    {
        var text = new StringBuilder();
        var buffer = new char[8192];
        int read;
        while ((read = stdin.Read(buffer, 0, buffer.Length)) > 0)
        {
            text.Append(buffer, 0, read);
            if (text.Length > MaxCharacters)
            {
                return Result<string>.Fail($"the journal is larger than {MaxCharacters / (1024 * 1024)} million characters");
            }
        }

        return Result<string>.Ok(text.ToString());
    }
}
