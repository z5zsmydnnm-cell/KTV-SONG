using KTVManagerProfessional.Core.Git;

namespace KTVManagerProfessional.Tests;

public sealed class GitStatusParserTests
{
    [Fact]
    public void ParsePorcelain_reads_common_statuses()
    {
        var output = """
         M songs/master.csv
        A  manager-v6/data/ktv.sqlite
         D old.txt
        ?? new-file.csv
        """;

        var entries = GitStatusParser.ParsePorcelain(output);

        Assert.Contains(entries, entry => entry.Path == "songs/master.csv" && entry.Status == GitFileStatus.Modified);
        Assert.Contains(entries, entry => entry.Path == "manager-v6/data/ktv.sqlite" && entry.Status == GitFileStatus.Added);
        Assert.Contains(entries, entry => entry.Path == "old.txt" && entry.Status == GitFileStatus.Deleted);
        Assert.Contains(entries, entry => entry.Path == "new-file.csv" && entry.Status == GitFileStatus.Untracked);
    }
}
