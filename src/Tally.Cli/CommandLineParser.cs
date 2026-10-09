using System.Globalization;
using Tally.Core;

namespace Tally.Cli;

/// <summary>Turns arguments into a <see cref="CommandLine"/>, or says what is wrong with them.</summary>
internal static class CommandLineParser
{
    private const int MaxDepth = 64;

    public static Result<CommandLine> Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return Result<CommandLine>.Fail("no command given");
        }

        if (Commands.Find(args[0]) is not { } command)
        {
            return Result<CommandLine>.Fail($"unknown command '{args[0]}'");
        }

        string? journal = null;
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var json = false;

        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "-" || !arg.StartsWith('-'))
            {
                if (journal is not null)
                {
                    return Result<CommandLine>.Fail($"unexpected extra argument '{arg}'; a command reads one journal");
                }

                journal = arg;
                continue;
            }

            var equals = arg.IndexOf('=', StringComparison.Ordinal);
            var name = equals < 0 ? arg : arg[..equals];
            var spec = Options.All.FirstOrDefault(option => option.Name == name);
            if (spec is null || !command.Options.Contains(name))
            {
                return Result<CommandLine>.Fail($"'{command.Name}' does not take the option '{name}'");
            }

            if (spec.ValueName is null)
            {
                if (equals >= 0)
                {
                    return Result<CommandLine>.Fail($"the option '{name}' takes no value");
                }

                json = true;
                continue;
            }

            if (equals >= 0)
            {
                values[name] = arg[(equals + 1)..];
            }
            else if (i + 1 < args.Length)
            {
                values[name] = args[++i];
            }
            else
            {
                return Result<CommandLine>.Fail($"the option '{name}' needs a {spec.ValueName}");
            }
        }

        if (journal is null)
        {
            return Result<CommandLine>.Fail($"'{command.Name}' needs a journal file; use - for standard input");
        }

        return Build(command, journal, values, json);
    }

    private static Result<CommandLine> Build(Command command, string journal, Dictionary<string, string> values, bool json)
    {
        AccountName? account = null;
        if (values.TryGetValue(Options.Account, out var accountText))
        {
            var parsed = AccountName.Parse(accountText);
            if (!parsed.IsOk)
            {
                return Result<CommandLine>.Fail($"{Options.Account}: {parsed.Error}");
            }

            account = parsed.Value;
        }

        var from = ParseDate(values, Options.From);
        var to = ParseDate(values, Options.To);
        if (!from.IsOk)
        {
            return Result<CommandLine>.Fail(from.Error);
        }

        if (!to.IsOk)
        {
            return Result<CommandLine>.Fail(to.Error);
        }

        if (from.Value > to.Value)
        {
            return Result<CommandLine>.Fail($"{Options.From} {from.Value:yyyy-MM-dd} is after {Options.To} {to.Value:yyyy-MM-dd}");
        }

        int? depth = null;
        if (values.TryGetValue(Options.Depth, out var depthText))
        {
            if (!int.TryParse(depthText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed is < 1 or > MaxDepth)
            {
                return Result<CommandLine>.Fail($"{Options.Depth} takes a whole number from 1 to {MaxDepth}, not '{depthText}'");
            }

            depth = parsed;
        }

        return Result<CommandLine>.Ok(new CommandLine(command, journal, account, from.Value, to.Value, depth, json));
    }

    private static Result<DateOnly?> ParseDate(Dictionary<string, string> values, string option)
    {
        if (!values.TryGetValue(option, out var text))
        {
            return Result<DateOnly?>.Ok(null);
        }

        return DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? Result<DateOnly?>.Ok(date)
            : Result<DateOnly?>.Fail($"{option} takes a date as YYYY-MM-DD, not '{text}'");
    }
}
