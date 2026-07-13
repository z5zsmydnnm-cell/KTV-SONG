using System.Text;
using KTVManagerProfessional.Core;

namespace KTVManagerProfessional.Tests;

public sealed class CsvExporterTests
{
    [Fact]
    public void ExportMasterCsv_writes_excel_friendly_multibrand_rows()
    {
        var path = Path.Combine(Path.GetTempPath(), $"master-{Guid.NewGuid():N}.csv");
        var songs = new[]
        {
            new SongRecord("123456", "愛情限時批", "伍佰, 萬芳", "台語", "音圓", "1326"),
            new SongRecord("654321", "愛情限時批", "伍佰, 萬芳", "台語", "金嗓", "GS01")
        };

        CsvExporter.ExportMasterCsv(path, songs);

        var bytes = File.ReadAllBytes(path);
        var content = Encoding.UTF8.GetString(bytes);

        Assert.True(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        Assert.Equal(
            "歌名,歌手,語言,音圓代號,金嗓代號,弘音代號,集數,備註\r\n愛情限時批,\"伍佰, 萬芳\",台語,123456,654321,,1326; GS01,\r\n",
            content.TrimStart('\uFEFF'));
    }

    [Fact]
    public void ExportBrandCsvs_writes_brand_specific_files_from_master_rows()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"brand-csv-{Guid.NewGuid():N}");
        var songs = new[]
        {
            new SongRecord("111111", "童話", "光良", "國語", "音圓", "1326"),
            new SongRecord("222222", "童話", "光良", "國語", "金嗓", "GS01"),
            new SongRecord("333333", "家後", "江蕙", "台語", "金嗓", "GS02")
        };

        CsvExporter.ExportBrandCsvs(directory, songs);

        var inYuan = Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(directory, "音圓.csv"))).TrimStart('\uFEFF');
        var goldenVoice = Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(directory, "金嗓.csv"))).TrimStart('\uFEFF');

        Assert.Equal(
            "歌名,歌手,語言,音圓代號,金嗓代號,弘音代號,集數,備註\r\n童話,光良,國語,111111,222222,,1326; GS01,\r\n",
            inYuan);
        Assert.Equal(
            "歌名,歌手,語言,音圓代號,金嗓代號,弘音代號,集數,備註\r\n童話,光良,國語,111111,222222,,1326; GS01,\r\n家後,江蕙,台語,,333333,,GS02,\r\n",
            goldenVoice);
    }
}
