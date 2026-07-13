using KTVManagerProfessional.Core.Importing;
using KTVManagerProfessional.Core.Ocr;
using KTVManagerProfessional.Core.Parsing;

namespace KTVManagerProfessional.Tests;

public sealed class GoldenVoiceOcrSongParserTests
{
    [Fact]
    public void ParsePages_rebuilds_two_column_golden_voice_rows()
    {
        var page = new OcrPage(
            PageNumber: 1,
            Width: 1654,
            Height: 2339,
            Words:
            [
                Word("國", 1381, 92), Word("語", 1431, 93), Word("歌", 1486, 90), Word("曲", 1548, 91),
                Word("37195", 164, 215), Word("雨", 321, 221), Word("中", 380, 215), Word("的", 431, 217), Word("思", 486, 218), Word("念", 540, 215),
                Word("祁", 726, 227), Word("隆", 752, 227),
                Word("59423", 909, 217), Word("誰", 1063, 217), Word("來", 1120, 215), Word("剪", 1174, 214), Word("月", 1232, 219), Word("光", 1287, 217),
                Word("陳", 1491, 227), Word("迅", 1540, 230)
            ]);

        var result = new GoldenVoiceOcrSongParser().ParsePages([page], "金嗓1.pdf");

        Assert.Empty(result.Issues);
        Assert.Contains(result.Songs, song =>
            song.BrandCode == BrandCode.GoldenVoice &&
            song.SongNumber == "37195" &&
            song.Title == "雨中的思念" &&
            song.Artist == "祁隆" &&
            song.Language == "國語" &&
            song.Volume == "1");
        Assert.Contains(result.Songs, song =>
            song.SongNumber == "59423" &&
            song.Title == "誰來剪月光" &&
            song.Artist == "陳迅");
    }

    [Fact]
    public void ParsePages_merges_song_numbers_split_by_ocr()
    {
        var page = new OcrPage(
            PageNumber: 1,
            Width: 1654,
            Height: 2339,
            Words:
            [
                Word("國", 1381, 92), Word("語", 1431, 93), Word("歌", 1486, 90), Word("曲", 1548, 91),
                Word("5", 163, 1423, 20), Word("9", 190, 1423, 21), Word("5", 218, 1423, 20), Word("1", 248, 1423, 14), Word("6", 273, 1423, 20),
                Word("造", 318, 1424), Word("天", 375, 1428), Word("氣", 432, 1423), Word("的", 486, 1424), Word("人", 539, 1427),
                Word("光", 726, 1434), Word("良", 752, 1434)
            ]);

        var result = new GoldenVoiceOcrSongParser().ParsePages([page], "金嗓1.pdf");

        var song = Assert.Single(result.Songs);
        Assert.Equal("59516", song.SongNumber);
        Assert.Equal("造天氣的人", song.Title);
        Assert.Equal("光良", song.Artist);
    }

    private static OcrWord Word(string text, double x, double y, double width = 45, double height = 45)
    {
        return new OcrWord(text, x, y, width, height);
    }
}
