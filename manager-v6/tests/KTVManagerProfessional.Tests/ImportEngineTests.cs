using KTVManagerProfessional.Core.Data;
using KTVManagerProfessional.Core.Importing;

namespace KTVManagerProfessional.Tests;

public sealed class ImportEngineTests
{
    [Fact]
    public async Task ImportFilesAsync_imports_csv_and_reports_unsupported_file()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ktv-import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var databasePath = Path.Combine(directory, "ktv.sqlite");
        var csvPath = Path.Combine(directory, "音圓1356.csv");
        var txtPath = Path.Combine(directory, "readme.txt");
        await File.WriteAllTextAsync(csvPath, "SongNumber,Title,Artist,Language,Volume\r\n123456,想你的夜,關喆,國語,1356\r\n");
        await File.WriteAllTextAsync(txtPath, "not a song file");
        KtvDatabase.Initialize(databasePath);

        var summary = await new ImportEngine().ImportFilesAsync([csvPath, txtPath], databasePath, CancellationToken.None);

        Assert.Equal(2, summary.TotalFiles);
        Assert.Equal(1, summary.TotalRows);
        Assert.Equal(1, summary.ImportedRows);
        Assert.Equal(1, summary.Results.Count(result => result.SourceType == ImportSourceType.Unsupported));
        Assert.Equal(1, new SongRepository(databasePath).CountSongs());
    }
}
