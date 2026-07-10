namespace KTVManagerProfessional.Core.Git;

public sealed record GitRepositorySnapshot(
    string RepositoryPath,
    string BranchName,
    string RemoteUrl,
    IReadOnlyList<GitStatusEntry> Entries,
    string Error);
