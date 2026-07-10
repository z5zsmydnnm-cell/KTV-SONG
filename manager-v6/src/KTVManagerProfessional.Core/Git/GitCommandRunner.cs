using System.Diagnostics;

namespace KTVManagerProfessional.Core.Git;

public sealed class GitCommandRunner
{
    public async Task<GitCommandResult> RunAsync(string repositoryPath, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Any(IsDestructive))
        {
            return new GitCommandResult(1, string.Empty, "Destructive git command is blocked.");
        }

        using var process = new Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.WorkingDirectory = repositoryPath;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var exitTask = process.WaitForExitAsync(cancellationToken);
        var completed = await Task.WhenAny(exitTask, Task.Delay(timeout, cancellationToken));
        if (completed != exitTask)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best-effort cleanup after timeout.
            }

            return new GitCommandResult(1, await stdoutTask, "Git command timed out.");
        }

        return new GitCommandResult(process.ExitCode, await stdoutTask, await stderrTask);
    }

    private static bool IsDestructive(string argument)
    {
        return string.Equals(argument, "--hard", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "clean", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "--force", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "-f", StringComparison.OrdinalIgnoreCase);
    }
}
