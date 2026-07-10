namespace KTVManagerProfessional.Core.Git;

public sealed record GitCommandResult(int ExitCode, string StdOut, string StdErr)
{
    public bool Succeeded => ExitCode == 0;
}
