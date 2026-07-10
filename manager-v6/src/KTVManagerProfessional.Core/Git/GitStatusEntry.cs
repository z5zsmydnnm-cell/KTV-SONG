namespace KTVManagerProfessional.Core.Git;

public sealed record GitStatusEntry(string Path, GitFileStatus Status, string RawStatus);
