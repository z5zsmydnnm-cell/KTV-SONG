using System.Text;
using KTVManagerProfessional.Core;

namespace KTVManagerProfessional.Tests;

public sealed class CsvExporterTests
{
    [Fact]
    public void ExportMasterCsv_writes_utf8_rows_with_header()
    {
        var path = Path.Combine(Path.GetTempPath(), $"master-{Guid.NewGuid():N}.csv");
        var songs = new[]
        {
            new SongRecord("123456", "愛情限時批", "伍佰, 萬芳", "台語", "音圓", "1326")
        };

        CsvExporter.ExportMasterCsv(path, songs);

        var bytes = File.ReadAllBytes(path);
        var content = Encoding.UTF8.GetString(bytes);

        Assert.True(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        Assert.Equal("歌號,歌名,歌手,語言,音圓代號,集數\r\n123456,愛情限時批,\"伍佰, 萬芳\",台語,音圓,1326\r\n", content.TrimStart('\uFEFF'));
    }
}
