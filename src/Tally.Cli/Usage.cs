using System.Text;

namespace Tally.Cli;

internal static class Usage
{
    public static string Text()
    {
        var text = new StringBuilder();
        text.AppendLine("tally - plain-text double-entry bookkeeping");
        text.AppendLine();
        text.AppendLine("usage: tally <command> [options] <journal>");
        text.AppendLine();
        text.AppendLine("<journal> is a file, or - for standard input.");
        text.AppendLine();
        text.AppendLine("commands:");
        foreach (var command in Commands.All)
        {
            text.AppendLine($"  {command.Name,-10}{command.Summary}");
        }

        text.AppendLine("  help      Show this text.");
        text.AppendLine();
        text.AppendLine("options:");
        foreach (var option in Options.All)
        {
            var left = option.ValueName is null ? option.Name : $"{option.Name} {option.ValueName}";
            text.AppendLine($"  {left,-18}{option.Summary}");
        }

        text.AppendLine();
        text.AppendLine("exit codes: 0 success, 1 the journal is unreadable or has problems, 2 bad command line");
        return text.ToString();
    }
}
