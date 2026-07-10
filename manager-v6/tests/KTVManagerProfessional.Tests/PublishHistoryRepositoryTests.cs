using KTVManagerProfessional.Core.Data;

namespace KTVManagerProfessional.Tests;

public sealed class PublishHistoryRepositoryTests
{
    [Fact]
    public void Add_records_publish_attempt()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ktv-{Guid.NewGuid():N}.sqlite");
        KtvDatabase.Initialize(path);
        var repository = new PublishHistoryRepository(path);

        repository.Add(new PublishHistoryRecord(
            Action: "Commit",
            RepositoryPath: @"D:\GitHub\KTV-SONG",
            BranchName: "agent/test",
            RemoteUrl: "https://github.com/example/repo.git",
            CommitSha: "abc123",
            Message: "data: update",
            SelectedFilesJson: "[\"songs/master.csv\"]",
            StartedAt: DateTimeOffset.Parse("2026-07-10T12:00:00Z"),
            FinishedAt: DateTimeOffset.Parse("2026-07-10T12:00:01Z"),
            ExitCode: 0,
            StdOut: "ok",
            StdErr: "",
            Status: "Succeeded"));

        Assert.Equal(1, repository.Count());
    }
}
