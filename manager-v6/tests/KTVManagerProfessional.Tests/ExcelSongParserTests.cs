using ClosedXML.Excel;
using KTVManagerProfessional.Core.Importing;
using KTVManagerProfessional.Core.Parsing;

namespace KTVManagerProfessional.Tests;

public sealed class ExcelSongParserTests
{
    [Fact]
    public void ParseFile_reads_first_worksheet_with_chinese_headers()
    {
        var path = Path.Combine(Path.GetTempPath(), $"songs-{Guid.NewGuid():N}.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("songs");
            sheet.Cell(1, 1).Value = "歌號";
            sheet.Cell(1, 2).Value = "歌名";
            sheet.Cell(1, 3).Value = "歌手";
            sheet.Cell(1, 4).Value = "語言";
            sheet.Cell(1, 5).Value = "集數";
            sheet.Cell(2, 1).Value = "654321";
            sheet.Cell(2, 2).Value = "想你的夜";
            sheet.Cell(2, 3).Value = "關喆";
            sheet.Cell(2, 4).Value = "國語";
            sheet.Cell(2, 5).Value = "1356";
            workbook.SaveAs(path);
        }

        var result = new ExcelSongParser().ParseFile(path, BrandCode.InYuan);

        Assert.Empty(result.Issues);
        var song = Assert.Single(result.Songs);
        Assert.Equal("654321", song.SongNumber);
        Assert.Equal("想你的夜", song.Title);
        Assert.Equal("關喆", song.Artist);
        Assert.Equal("國語", song.Language);
        Assert.Equal("1356", song.Volume);
    }
}
