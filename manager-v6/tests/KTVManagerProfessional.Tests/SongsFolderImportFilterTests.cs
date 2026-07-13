using KTVManagerProfessional.Core;

namespace KTVManagerProfessional.Tests;

public sealed class SongsFolderImportFilterTests
{
    [Theory]
    [InlineData("master.csv")]
    [InlineData("音圓.csv")]
    [InlineData("金嗓.csv")]
    [InlineData("弘音.csv")]
    public void IsImportSourceFile_excludes_generated_song_export_files(string fileName)
    {
        var path = CreateTempFile(fileName);

        Assert.False(SongsFolderImportFilter.IsImportSourceFile(path));
    }

    [Theory]
    [InlineData("1140.pdf")]
    [InlineData("custom.csv")]
    [InlineData("iphone-local-songs.csv")]
    [InlineData("songs.xlsx")]
    public void IsImportSourceFile_allows_real_import_sources(string fileName)
    {
        var path = CreateTempFile(fileName);

        Assert.True(SongsFolderImportFilter.IsImportSourceFile(path));
    }

    private static string CreateTempFile(string fileName)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ktv-filter-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, string.Empty);
        return path;
    }
}
