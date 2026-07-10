namespace KTVManagerProfessional.Core.Data;

public sealed record PublishHistoryRecord(
    string Action,
    string RepositoryPath,
    string? BranchName,
    string? RemoteUrl,
    string? CommitSha,
    string? Message,
    string? SelectedFilesJson,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int? ExitCode,
    string? StdOut,
    string? StdErr,
    string Status);
