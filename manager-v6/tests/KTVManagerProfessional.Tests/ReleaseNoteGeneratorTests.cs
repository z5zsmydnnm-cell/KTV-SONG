using KTVManagerProfessional.Core.Git;
using KTVManagerProfessional.Core.Importing;

namespace KTVManagerProfessional.Tests;

public sealed class ReleaseNoteGeneratorTests
{
    [Fact]
    public void Generate_contains_import_counts_and_success_rate()
    {
        var summary = ImportSummary.FromResults([
            new ImportFileResult("a.csv", "a.csv", ImportSourceType.Csv, "音圓", "1356", 10, 5, 2, 1, 2, [])
        ]);

        var markdown = ReleaseNoteGenerator.Generate(summary, "songs/master.csv", new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));

        Assert.Contains("New songs: 5", markdown);
        Assert.Contains("Updated songs: 2", markdown);
        Assert.Contains("Duplicates skipped: 1", markdown);
        Assert.Contains("Failed rows: 2", markdown);
        Assert.Contains("Success rate: 80.0%", markdown);
        Assert.Contains("songs/master.csv", markdown);
    }
}
