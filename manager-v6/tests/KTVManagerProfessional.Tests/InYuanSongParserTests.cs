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
    public void ParseText_accepts_single_column_song_without_artist()
    {
        var text = """
        ?喳? 3011
        ?啗?
        202569 你就是方向
        """;

        var result = InYuanSongParser.ParseText(text, "3011.pdf");

        Assert.Empty(result.Issues);
        var song = Assert.Single(result.Songs);
        Assert.Equal("202569", song.SongNumber);
        Assert.Equal("你就是方向", song.Title);
        Assert.Equal(string.Empty, song.Artist);
        Assert.Equal(string.Empty, song.Language);
        Assert.Equal("3011", song.Volume);
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

    [Fact]
    public void ParseText_reports_when_no_song_rows_match()
    {
        var text = """
        1102
        header text only
        no five digit song rows here
        """;

        var result = InYuanSongParser.ParseText(text, "1102.pdf");

        Assert.Empty(result.Songs);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(0, issue.LineNumber);
        Assert.Contains("No song rows matched", issue.Reason);
    }

    [Fact]
    public void ParseText_supports_old_four_digit_inyuan_catalog_rows()
    {
        var text = """
        Singing and you will see!!
        Song No Title Language Artist Rhythm
        ● 6434 舞伴 台 袁小迪 Tango
        ● 3707 Little Darling 國 林國榮 Slow Soul
        台語歌曲 【 第 1102 集專輯 】
        """;

        var result = InYuanSongParser.ParseText(text, "1102.pdf");

        Assert.Empty(result.Issues);
        Assert.Equal(2, result.Songs.Count);
        Assert.Contains(new SongRecord("6434", "舞伴", "袁小迪", "台語", "音圓", "1102"), result.Songs);
        Assert.Contains(new SongRecord("3707", "Little Darling", "林國榮", "國語", "音圓", "1102"), result.Songs);
    }

    [Fact]
    public void ParseText_supports_old_inyuan_rows_split_by_itext_extraction()
    {
        var text = """
        Singing and you will see!
        ●
        舞伴 台 袁小迪 Tango
        6434
        ●
        七郎 Soul
        爽就好 台
        6162
        ●
        林國榮 Slow Soul
        Little Darling 國
        3707
        ○
        狼 國語 齊秦 Slow Soul
        3516
        台語歌曲
        【 第 1102集專輯 】
        """;

        var result = InYuanSongParser.ParseText(text, "1102.pdf");

        Assert.Empty(result.Issues);
        Assert.Equal(4, result.Songs.Count);
        Assert.Contains(new SongRecord("6434", "舞伴", "袁小迪", "台語", "音圓", "1102"), result.Songs);
        Assert.Contains(new SongRecord("6162", "爽就好", "七郎", "台語", "音圓", "1102"), result.Songs);
        Assert.Contains(new SongRecord("3707", "Little Darling", "林國榮", "國語", "音圓", "1102"), result.Songs);
        Assert.Contains(new SongRecord("3516", "狼", "齊秦", "國語", "音圓", "1102"), result.Songs);
    }

    [Fact]
    public void ParseText_supports_old_inyuan_rows_with_separate_language_lines()
    {
        var text = """
        台語
        ○
        往事 黃乙玲 Slow Soul
        6359
        台語
        ○
        王建傑.詹曼鈴 Slow Soul
        夢中網
        6366
        ○ 台語
        慕鈺華 Tango
        6351 無心花
        MIDI 專輯(第 1012集)
        """;

        var result = InYuanSongParser.ParseText(text, "1012.pdf");

        Assert.Empty(result.Issues);
        Assert.Equal(3, result.Songs.Count);
        Assert.Contains(new SongRecord("6359", "往事", "黃乙玲", "台語", "音圓", "1012"), result.Songs);
        Assert.Contains(new SongRecord("6366", "夢中網", "王建傑.詹曼鈴", "台語", "音圓", "1012"), result.Songs);
        Assert.Contains(new SongRecord("6351", "無心花", "慕鈺華", "台語", "音圓", "1012"), result.Songs);
    }

    [Fact]
    public void ParseText_keeps_five_digit_old_catalog_numbers_with_their_titles()
    {
        var text = """
        ●
        羅美玲 勃露斯
        不痛多好 國
        50150
        ●
        四面楚歌 國 周杰倫 梭
        50072
        ●
        用情太深 國 邰正宵 勃露斯
        5905
        MIDI 專輯(第 1140集)
        """;

        var result = InYuanSongParser.ParseText(text, "1140.pdf");

        Assert.Empty(result.Issues);
        Assert.Contains(new SongRecord("50072", "四面楚歌", "周杰倫", "國語", "音圓", "1140"), result.Songs);
        Assert.DoesNotContain(result.Songs, song => song.SongNumber == "50150" && song.Title.Contains("四面楚歌", StringComparison.Ordinal));
    }

    [Fact]
    public void ParseText_supports_6107_old_catalog_blocks_with_control_marker_and_number_first()
    {
        var text = """
        
        55966
        王建傑 勃露斯
        成全 台
         詹曼鈴/沈建豪 勃露斯
        55961 背後 台
        """;

        var result = InYuanSongParser.ParseText(text, "6107.pdf");

        Assert.Empty(result.Issues);
        Assert.Contains(new SongRecord("55966", "成全", "王建傑", "台語", "音圓", "6107"), result.Songs);
        Assert.Contains(new SongRecord("55961", "背後", "詹曼鈴/沈建豪", "台語", "音圓", "6107"), result.Songs);
    }

    [Fact]
    public void ParseText_strips_chinese_rhythm_suffixes_from_old_catalog_artists()
    {
        var text = """
        
        羅百吉/寶貝 迪斯可
        台灣女孩 國
        51023
        """;

        var result = InYuanSongParser.ParseText(text, "6107.pdf");

        Assert.Empty(result.Issues);
        Assert.Contains(new SongRecord("51023", "台灣女孩", "羅百吉/寶貝", "國語", "音圓", "6107"), result.Songs);
    }
}
