using System.Collections.Immutable;

namespace Tally.Cli;

internal static class Commands
{
    public static ImmutableArray<Command> All { get; } =
    [
        new("check", "Check a journal and report every problem.", [Options.Json], true, CheckCommand.Run),
        new("balance", "Show what each account holds.", [Options.Account, Options.From, Options.To, Options.Depth, Options.Json], false, BalanceCommand.Run),
        new("register", "List postings with a running balance.", [Options.Account, Options.From, Options.To, Options.Json], false, RegisterCommand.Run),
        new("accounts", "List the accounts that have postings.", [Options.Json], false, AccountsCommand.Run),
        new("print", "Write the journal in canonical form.", [], false, PrintCommand.Run),
    ];

    public static Command? Find(string name) => All.FirstOrDefault(command => command.Name == name);
}
