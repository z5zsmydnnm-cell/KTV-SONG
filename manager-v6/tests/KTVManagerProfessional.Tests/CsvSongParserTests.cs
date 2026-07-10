using KTVManagerProfessional.Core.Importing;
using KTVManagerProfessional.Core.Parsing;

namespace KTVManagerProfessional.Tests;

public sealed class CsvSongParserTests
{
    [Fact]
    public void ParseFile_reads_english_headers_and_quoted_fields()
    {
        var path = Path.Combine(Path.GetTempPath(), $"songs-{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, "SongNumber,Title,Artist,Language,Volume\r\n123456,\"Love, Song\",Singer,國語,1356\r\n");

        var result = new CsvSongParser().ParseFile(path, BrandCode.InYuan);

        Assert.Empty(result.Issues);
        var song = Assert.Single(result.Songs);
        Assert.Equal("123456", song.SongNumber);
        Assert.Equal("Love, Song", song.Title);
        Assert.Equal("Singer", song.Artist);
        Assert.Equal("國語", song.Language);
        Assert.Equal("1356", song.Volume);
        Assert.Equal(BrandCode.InYuan, song.BrandCode);
    }
}
