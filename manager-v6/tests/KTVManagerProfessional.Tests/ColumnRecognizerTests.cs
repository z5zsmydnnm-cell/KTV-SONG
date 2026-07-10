using KTVManagerProfessional.Core.Parsing;

namespace KTVManagerProfessional.Tests;

public sealed class ColumnRecognizerTests
{
    [Fact]
    public void Recognize_maps_chinese_headers()
    {
        var map = ColumnRecognizer.Recognize(["歌號", "歌名", "歌手", "語言", "集數"], []);

        Assert.Equal(0, map.SongNumberIndex);
        Assert.Equal(1, map.TitleIndex);
        Assert.Equal(2, map.ArtistIndex);
        Assert.Equal(3, map.LanguageIndex);
        Assert.Equal(4, map.VolumeIndex);
    }

    [Fact]
    public void Recognize_maps_english_headers()
    {
        var map = ColumnRecognizer.Recognize(["SongNumber", "Title", "Artist", "Language", "Volume"], []);

        Assert.Equal(0, map.SongNumberIndex);
        Assert.Equal(1, map.TitleIndex);
        Assert.Equal(2, map.ArtistIndex);
        Assert.Equal(3, map.LanguageIndex);
        Assert.Equal(4, map.VolumeIndex);
    }
}
