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

    [Fact]
    public void ParseFile_reads_iphone_local_song_codes_and_skips_rows_without_codes()
    {
        var path = Path.Combine(Path.GetTempPath(), $"iphone-local-songs-{Guid.NewGuid():N}.csv");
        File.WriteAllText(
            path,
            "歌名,歌手,語言,音圓代號,金嗓代號,弘音代號,YouTube,備註\r\n" +
            "一個人,,,,,,https://youtu.be/example,\r\n" +
            "一次就好,,國語,,1233,,,\r\n");

        var result = new CsvSongParser().ParseFile(path, BrandCode.InYuan);

        Assert.Empty(result.Issues);
        var song = Assert.Single(result.Songs);
        Assert.Equal("1233", song.SongNumber);
        Assert.Equal("一次就好", song.Title);
        Assert.Equal("國語", song.Language);
        Assert.Equal(BrandCode.GoldenVoice, song.BrandCode);
    }

    [Fact]
    public void ParseFile_reads_utf8_bom_master_csv_headers()
    {
        var path = Path.Combine(Path.GetTempPath(), $"master-{Guid.NewGuid():N}.csv");
        File.WriteAllText(
            path,
            "\uFEFF歌名,歌手,語言,音圓代號,金嗓代號,弘音代號,集數,備註\r\n" +
            "測試歌,測試歌手,台語,201811,301234,,1356,\r\n");

        var result = new CsvSongParser().ParseFile(path, BrandCode.Unknown);

        Assert.Empty(result.Issues);
        Assert.Equal(2, result.Songs.Count);
        Assert.Contains(result.Songs, song => song.BrandCode == BrandCode.InYuan && song.SongNumber == "201811");
        Assert.Contains(result.Songs, song => song.BrandCode == BrandCode.GoldenVoice && song.SongNumber == "301234");
    }
}
