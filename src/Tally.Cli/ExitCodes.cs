namespace Tally.Cli;

/// <summary>What <c>tally</c> returns; each value means one thing.</summary>
internal static class ExitCodes
{
    public const int Ok = 0;

    /// <summary>The journal could not be read or has problems.</summary>
    public const int InvalidJournal = 1;

    /// <summary>The command line is wrong.</summary>
    public const int Usage = 2;
}
