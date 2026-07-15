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

    [Fact]
    public void ParsePages_detects_japanese_pages()
    {
        var page = new OcrPage(
            PageNumber: 1,
            Width: 1984,
            Height: 2806,
            Words:
            [
                Word("日", 961, 96), Word("語", 1038, 91), Word("歌", 1133, 86), Word("曲", 1238, 86),
                Word("40451", 162, 585, 164, 55),
                Word("あ", 338, 586), Word("き", 401, 586), Word("ら", 463, 586), Word("め", 525, 586), Word("て", 587, 586)
            ]);

        var result = new GoldenVoiceOcrSongParser().ParsePages([page], "金嗓4.pdf");

        var song = Assert.Single(result.Songs);
        Assert.Equal("日語", song.Language);
        Assert.Equal("40451", song.SongNumber);
        Assert.Equal("あきらめて", song.Title);
    }

    [Fact]
    public void ParsePages_normalizes_ocr_confusion_in_dotted_io_titles()
    {
        var page = new OcrPage(
            PageNumber: 1,
            Width: 1984,
            Height: 2806,
            Words:
            [
                Word("國", 1650, 80), Word("語", 1710, 80),
                Word("16458", 170, 980, 160, 55),
                Word("1.0", 355, 980, 105, 55), Word("·", 470, 992, 18, 24), Word("I.0", 500, 980, 105, 55)
            ]);

        var result = new GoldenVoiceOcrSongParser().ParsePages([page], "金嗓2.pdf");

        var song = Assert.Single(result.Songs);
        Assert.Equal("16458", song.SongNumber);
        Assert.Equal("I.O.I.O", song.Title);
    }

    [Fact]
    public void ParsePages_splits_song_number_and_title_when_ocr_merges_them()
    {
        var page = new OcrPage(
            PageNumber: 1,
            Width: 1984,
            Height: 2806,
            Words:
            [
                Word("01412AY0", 167, 2048, 286, 53),
                Word("溫", 873, 2056, 28, 34), Word("嵐", 905, 2053, 28, 34)
            ]);

        var result = new GoldenVoiceOcrSongParser().ParsePages([page], "金嗓2.pdf");

        var song = Assert.Single(result.Songs);
        Assert.Empty(result.Issues);
        Assert.Equal("01412", song.SongNumber);
        Assert.Equal("AY0", song.Title);
        Assert.Equal("溫嵐", song.Artist);
    }

    [Fact]
    public void ParsePages_uses_better_overlapping_ocr_alternatives_for_title()
    {
        var page = new OcrPage(
            PageNumber: 17,
            Width: 1984,
            Height: 2806,
            Words:
            [
                Word("10591", 164, 1570, 160, 52),
                Word("和", 348, 1571), Word("イ", 348, 1571), Word("。", 370, 1571),
                Word("何", 349, 1571), Word("日", 431, 1571), Word("君", 485, 1571),
                Word("再", 552, 1571), Word("∕", 627, 1571), Word("來", 626, 1571),
                Word("鄧", 865, 1571), Word("麗", 898, 1571), Word("君", 928, 1571)
            ]);

        var result = new GoldenVoiceOcrSongParser().ParsePages([page], "金嗓3.pdf");

        var song = Assert.Single(result.Songs);
        Assert.Equal("10591", song.SongNumber);
        Assert.Equal("何日君再來", song.Title);
        Assert.Equal("鄧麗君", song.Artist);
    }

    [Fact]
    public void ParsePages_keeps_title_words_near_column_start()
    {
        var page = new OcrPage(
            PageNumber: 1,
            Width: 1984,
            Height: 2806,
            Words:
            [
                Word("58717", 153, 2651, 161, 53),
                Word("LEFT", 330, 2651, 40, 45),
                Word("07488", 1089, 1136, 161, 54),
                Word("RIGHT", 1210, 1136, 50, 45)
            ]);

        var result = new GoldenVoiceOcrSongParser().ParsePages([page], "??5.pdf");

        Assert.Empty(result.Issues);
        Assert.Contains(result.Songs, song => song.SongNumber == "58717" && song.Title == "LEFT");
        Assert.Contains(result.Songs, song => song.SongNumber == "07488" && song.Title == "RIGHT");
    }

    [Fact]
    public void ParsePages_removes_single_digit_ocr_marker_before_cjk_title()
    {
        var page = new OcrPage(
            PageNumber: 1,
            Width: 1984,
            Height: 2806,
            Words:
            [
                Word("25055", 1089, 1136, 161, 54),
                Word("5", 1210, 1136, 20, 45),
                Word("\u6709", 1278, 1136, 30, 45),
                Word("\u60c5", 1347, 1136, 30, 45),
                Word("\u4eba", 1418, 1136, 30, 45)
            ]);

        var result = new GoldenVoiceOcrSongParser().ParsePages([page], "??5.pdf");

        var song = Assert.Single(result.Songs);
        Assert.Equal("25055", song.SongNumber);
        Assert.Equal("\u6709\u60c5\u4eba", song.Title);
    }

    [Fact]
    public void ParsePages_rejects_numeric_only_ocr_title()
    {
        var page = new OcrPage(
            PageNumber: 1,
            Width: 1984,
            Height: 2806,
            Words:
            [
                Word("07488", 1089, 1136, 161, 54),
                Word("8", 1210, 1136, 20, 45)
            ]);

        var result = new GoldenVoiceOcrSongParser().ParsePages([page], "??5.pdf");

        Assert.Empty(result.Songs);
        Assert.Contains(result.Issues, issue => issue.Line == "07488");
    }

    [Fact]
    public void ParsePages_trims_trailing_ocr_noise_from_six_digit_song_number()
    {
        var page = new OcrPage(
            PageNumber: 1,
            Width: 1984,
            Height: 2806,
            Words:
            [
                Word("463563", 164, 1136, 190, 54),
                Word("\u6642\u9593", 355, 1136, 60, 45),
                Word("\u6709\u6dda", 455, 1136, 60, 45),
                Word("\u5f35\u5b78\u53cb", 866, 1136, 70, 45)
            ]);

        var result = new GoldenVoiceOcrSongParser().ParsePages([page], "金嗓1.pdf");

        var song = Assert.Single(result.Songs);
        Assert.Equal("46356", song.SongNumber);
        Assert.Equal("\u6642\u9593\u6709\u6dda", song.Title);
        Assert.Equal("\u5f35\u5b78\u53cb", song.Artist);
    }

    [Fact]
    public void ParsePages_normalizes_common_piaobo_title_ocr_confusion()
    {
        var page = new OcrPage(
            PageNumber: 1,
            Width: 1984,
            Height: 2806,
            Words:
            [
                Word("28889", 164, 1136, 160, 54),
                Word("\u7968\u6cca", 355, 1136, 60, 45),
                Word("\u884c\u8239\u4eba", 455, 1136, 90, 45),
                Word("\u77f3\u55ac", 866, 1136, 70, 45)
            ]);

        var result = new GoldenVoiceOcrSongParser().ParsePages([page], "???7.pdf");

        var song = Assert.Single(result.Songs);
        Assert.Equal("28889", song.SongNumber);
        Assert.Equal("\u6f02\u6cca\u884c\u8239\u4eba", song.Title);
        Assert.Equal("\u77f3\u55ac", song.Artist);
        Assert.Equal("7", song.Volume);
    }

    [Fact]
    public void ParsePages_normalizes_common_yizhenfeng_title_ocr_confusion()
    {
        var page = new OcrPage(
            PageNumber: 1,
            Width: 1984,
            Height: 2806,
            Words:
            [
                Word("47421", 164, 1136, 160, 54),
                Word("\u611b\u611b\u5fd9\u60c5", 355, 1136, 120, 45),
                Word("\u4e00\u961d\u9663\u98a8\u98a8", 510, 1136, 120, 45),
                Word("\u5510\u5137", 866, 1136, 70, 45)
            ]);

        var result = new GoldenVoiceOcrSongParser().ParsePages([page], "???8.pdf");

        var song = Assert.Single(result.Songs);
        Assert.Equal("47421", song.SongNumber);
        Assert.Equal("\u611b\u60c5\u4e00\u9663\u98a8", song.Title);
        Assert.Equal("\u5510\u5137", song.Artist);
        Assert.Equal("8", song.Volume);
    }

    [Fact]
    public void ParsePages_removes_multiple_leading_ocr_markers_before_cjk_title()
    {
        var page = new OcrPage(
            PageNumber: 1,
            Width: 1984,
            Height: 2806,
            Words:
            [
                Word("46603", 164, 1136, 160, 54),
                Word(")3)3", 355, 1136, 80, 45),
                Word("\u5abd\u5abd\u8acb\u4f60\u514d\u639b\u5fc3", 455, 1136, 180, 45)
            ]);

        var result = new GoldenVoiceOcrSongParser().ParsePages([page], "???8.pdf");

        var song = Assert.Single(result.Songs);
        Assert.Equal("46603", song.SongNumber);
        Assert.Equal("\u5abd\u5abd\u8acb\u4f60\u514d\u639b\u5fc3", song.Title);
    }

    [Fact]
    public void ParsePages_does_not_include_right_column_star_in_left_artist()
    {
        var page = new OcrPage(
            PageNumber: 17,
            Width: 1984,
            Height: 2806,
            Words:
            [
                Word("48434", 192, 1430, 160, 52),
                Word("住", 418, 1430), Word("在", 499, 1430), Word("心", 584, 1430), Word("裡", 661, 1430), Word("面", 752, 1430),
                Word("吳", 866, 1430), Word("克", 897, 1430), Word("群", 928, 1430),
                Word("*", 1028, 1430)
            ]);

        var result = new GoldenVoiceOcrSongParser().ParsePages([page], "金嗓3.pdf");

        var song = Assert.Single(result.Songs);
        Assert.Equal("住在心裡面", song.Title);
        Assert.Equal("吳克群", song.Artist);
    }

    [Fact]
    public void ParsePages_does_not_include_next_row_artist_words()
    {
        var page = new OcrPage(
            PageNumber: 17,
            Width: 1984,
            Height: 2806,
            Words:
            [
                Word("56703", 194, 1665, 160, 52),
                Word("何", 417, 1665), Word("日", 517, 1665), Word("君", 581, 1665), Word("回", 669, 1665), Word("來", 749, 1665),
                Word("于", 866, 1665), Word("立", 897, 1665), Word("成", 928, 1665),
                Word("情", 866, 1713), Word("歌", 897, 1713), Word("對", 928, 1713)
            ]);

        var result = new GoldenVoiceOcrSongParser().ParsePages([page], "金嗓3.pdf");

        var song = Assert.Single(result.Songs);
        Assert.Equal("何日君回來", song.Title);
        Assert.Equal("于立成", song.Artist);
    }

    private static OcrWord Word(string text, double x, double y, double width = 45, double height = 45)
    {
        return new OcrWord(text, x, y, width, height);
    }
}
