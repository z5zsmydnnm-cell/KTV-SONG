using KTVManagerProfessional.Core;

namespace KTVManagerProfessional.Tests;

public sealed class InYuanSongParserTests
{
    [Fact]
    public void ParseText_supports_1326_symbol_format_and_multiline_fields()
    {
        var text = """
        音圓 1326
        台語新歌
        ◆ 123456 心內的雨
        落袂停  王識賢
        勃露斯
        ◆ 123457 愛情限時批  伍佰 萬芳
        音圓唱片
        """;

        var result = InYuanSongParser.ParseText(text, "音圓1326.pdf");

        Assert.Empty(result.Issues);
        Assert.Equal(2, result.Songs.Count);
        Assert.Equal(new SongRecord("123456", "心內的雨 落袂停", "王識賢", "台語", "音圓", "1326"), result.Songs[0]);
        Assert.Equal(new SongRecord("123457", "愛情限時批", "伍佰 萬芳", "台語", "音圓", "1326"), result.Songs[1]);
    }

    [Fact]
    public void ParseText_supports_1356_format_without_prefix_symbol()
    {
        var text = """
        第1356集 音圓新歌
        國語
        654321 想你的夜  關喆
        華語新歌
        654322 下一站
        幸福  梁靜茹
        客語
        654323 客家本色  羅時豐
        Shuffle
        """;

        var result = InYuanSongParser.ParseText(text, "音圓1356.pdf");

        Assert.Empty(result.Issues);
        Assert.Equal(3, result.Songs.Count);
        Assert.Equal("654321", result.Songs[0].SongNumber);
        Assert.Equal("想你的夜", result.Songs[0].Title);
        Assert.Equal("關喆", result.Songs[0].Artist);
        Assert.Equal("國語", result.Songs[0].Language);
        Assert.Equal("1356", result.Songs[0].Volume);
        Assert.Equal(new SongRecord("654322", "下一站 幸福", "梁靜茹", "華語", "音圓", "1356"), result.Songs[1]);
        Assert.Equal("客語", result.Songs[2].Language);
    }

    [Fact]
    public void ParseText_reports_song_like_lines_that_cannot_be_parsed()
    {
        var text = """
        音圓 1356
        國語
        777777
        777778 正常歌曲  正常歌手
        """;

        var result = InYuanSongParser.ParseText(text, "音圓1356.pdf");

        var issue = Assert.Single(result.Issues);
        Assert.Equal(3, issue.LineNumber);
        Assert.Equal("777777", issue.Line);
        Assert.Contains("missing title or artist", issue.Reason);
        Assert.Single(result.Songs);
    }

    [Fact]
    public void ParseText_splits_1326_double_column_lines_at_each_song_number()
    {
        var text = """
        音圓 1326
        台語
        201799 快醒雪 202528 美麗與哀愁
        202519 無字情歌 200853 寸草心
        202547 愛情支票 200863 這一刻
        """;

        var result = InYuanSongParser.ParseText(text, "1326.pdf");

        Assert.Empty(result.Issues);
        Assert.Equal(6, result.Songs.Count);
        Assert.Contains(new SongRecord("200853", "寸草心", string.Empty, "台語", "音圓", "1326"), result.Songs);
        Assert.Contains(new SongRecord("201799", "快醒雪", string.Empty, "台語", "音圓", "1326"), result.Songs);
        Assert.Contains(new SongRecord("202528", "美麗與哀愁", string.Empty, "台語", "音圓", "1326"), result.Songs);
    }
}
