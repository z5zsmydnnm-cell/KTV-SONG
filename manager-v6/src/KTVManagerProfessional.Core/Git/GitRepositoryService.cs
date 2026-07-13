using KTVManagerProfessional.Core.Data;

namespace KTVManagerProfessional.Core.Git;

public sealed class GitRepositoryService
{
    private readonly string _repositoryPath;
    private readonly string _databasePath;
    private readonly GitCommandRunner _runner;

    public GitRepositoryService(string repositoryPath, string databasePath, GitCommandRunner? runner = null)
    {
        _repositoryPath = repositoryPath;
        _databasePath = databasePath;
        _runner = runner ?? new GitCommandRunner();
    }

    public async Task<GitRepositorySnapshot> GetStatusAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_repositoryPath) || !Directory.Exists(Path.Combine(_repositoryPath, ".git")))
        {
            return new GitRepositorySnapshot(_repositoryPath, string.Empty, string.Empty, [], "Not a Git repository.");
        }

        var branch = await _runner.RunAsync(_repositoryPath, ["branch", "--show-current"], TimeSpan.FromSeconds(10), cancellationToken);
        var remote = await _runner.RunAsync(_repositoryPath, ["remote", "get-url", "origin"], TimeSpan.FromSeconds(10), cancellationToken);
        var status = await _runner.RunAsync(_repositoryPath, ["status", "--porcelain=v1"], TimeSpan.FromSeconds(10), cancellationToken);
        var error = string.Join(Environment.NewLine, new[] { branch.StdErr, remote.StdErr, status.StdErr }.Where(text => !string.IsNullOrWhiteSpace(text)));

        return new GitRepositorySnapshot(
            _repositoryPath,
            branch.StdOut.Trim(),
            remote.StdOut.Trim(),
            GitStatusParser.ParsePorcelain(status.StdOut),
            error);
    }

    public async Task<GitCommandResult> CommitAsync(IEnumerable<string> selectedFiles, string message, CancellationToken cancellationToken)
    {
        var files = selectedFiles
            .Where(ApplicationOwnedPathFilter.IsApplicationOwned)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            return new GitCommandResult(1, string.Empty, "No application-owned files selected.");
        }

        foreach (var file in files)
        {
            var addResult = await _runner.RunAsync(_repositoryPath, ["add", file], TimeSpan.FromSeconds(15), cancellationToken);
            if (!addResult.Succeeded)
            {
                return addResult;
            }
        }

        var started = DateTimeOffset.Now;
        var commit = await _runner.RunAsync(_repositoryPath, ["commit", "-m", message], TimeSpan.FromSeconds(30), cancellationToken);
        AddHistory("Commit", message, files, started, commit);
        return commit;
    }

    public async Task<GitCommandResult> PushAsync(CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.Now;
        var fetch = await _runner.RunAsync(_repositoryPath, ["fetch", "origin"], TimeSpan.FromSeconds(60), cancellationToken);
        if (!fetch.Succeeded)
        {
            AddHistory("Push", null, [], started, fetch);
            return fetch;
        }

        var branch = await _runner.RunAsync(_repositoryPath, ["branch", "--show-current"], TimeSpan.FromSeconds(10), cancellationToken);
        if (!branch.Succeeded || string.IsNullOrWhiteSpace(branch.StdOut))
        {
            var failed = CombineGitResults(fetch, branch);
            AddHistory("Push", null, [], started, failed);
            return failed;
        }

        var branchName = branch.StdOut.Trim();
        var rebase = await _runner.RunAsync(
            _repositoryPath,
            ["rebase", "--autostash", $"origin/{branchName}"],
            TimeSpan.FromSeconds(60),
            cancellationToken);
        if (!rebase.Succeeded)
        {
            var failed = CombineGitResults(fetch, branch, rebase);
            AddHistory("Push", null, [], started, failed);
            return failed;
        }

        var push = await _runner.RunAsync(_repositoryPath, ["push"], TimeSpan.FromSeconds(60), cancellationToken);
        var result = CombineGitResults(fetch, rebase, push);
        AddHistory("Push", null, [], started, result);
        return result;
    }

    private static GitCommandResult CombineGitResults(params GitCommandResult[] results)
    {
        var exitCode = results.LastOrDefault(result => !result.Succeeded)?.ExitCode ?? 0;
        var stdout = string.Join(
            Environment.NewLine,
            results.Select(result => result.StdOut).Where(text => !string.IsNullOrWhiteSpace(text)));
        var stderr = string.Join(
            Environment.NewLine,
            results.Select(result => result.StdErr).Where(text => !string.IsNullOrWhiteSpace(text)));
        return new GitCommandResult(exitCode, stdout, stderr);
    }

    private void AddHistory(string action, string? message, IReadOnlyList<string> files, DateTimeOffset started, GitCommandResult result)
    {
        KtvDatabase.Initialize(_databasePath);
        new PublishHistoryRepository(_databasePath).Add(new PublishHistoryRecord(
            Action: action,
            RepositoryPath: _repositoryPath,
            BranchName: null,
            RemoteUrl: null,
            CommitSha: null,
            Message: message,
            SelectedFilesJson: files.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(files),
            StartedAt: started,
            FinishedAt: DateTimeOffset.Now,
            ExitCode: result.ExitCode,
            StdOut: result.StdOut,
            StdErr: result.StdErr,
            Status: result.Succeeded ? "Succeeded" : "Failed"));
    }
}
