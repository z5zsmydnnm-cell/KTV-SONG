using KTVManagerProfessional.Core.Importing;
using KTVManagerProfessional.Core.Parsing;

namespace KTVManagerProfessional.Tests;

public sealed class GoldenVoicePdfSongParserTests
{
    [Fact]
    public void Parse_reads_basic_golden_voice_text_without_using_inyuan_brand()
    {
        var text = """
        金嗓 112
        國語
        12345 月亮代表我的心  鄧麗君
        12346 心事誰人知  沈文程
        """;

        var result = new GoldenVoicePdfSongParser().Parse(text, "金嗓112.pdf");

        Assert.Empty(result.Issues);
        Assert.Equal(2, result.Songs.Count);
        Assert.Contains(result.Songs, song =>
            song.BrandCode == BrandCode.GoldenVoice &&
            song.SongNumber == "12345" &&
            song.Title == "月亮代表我的心" &&
            song.Artist == "鄧麗君");
    }
}
