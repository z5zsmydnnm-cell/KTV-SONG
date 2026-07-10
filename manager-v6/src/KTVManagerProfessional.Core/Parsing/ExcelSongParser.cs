using ClosedXML.Excel;

namespace KTVManagerProfessional.Core.Parsing;

public sealed class ExcelSongParser
{
    public ParseResult ParseFile(string path, string brandCode)
    {
        using var workbook = new XLWorkbook(path);
        var worksheet = workbook.Worksheets.First();
        var range = worksheet.RangeUsed();
        if (range is null)
        {
            return new ParseResult([], [new ParseIssue(0, string.Empty, "Excel worksheet is empty.")]);
        }

        var table = range.Rows()
            .Select(row => row.Cells().Select(cell => cell.GetFormattedString()).ToList())
            .Cast<IReadOnlyList<string>>()
            .ToList();

        return table.Count == 0
            ? new ParseResult([], [new ParseIssue(0, string.Empty, "Excel worksheet is empty.")])
            : TabularSongParser.Parse(table[0], table.Skip(1).ToList(), brandCode, Path.GetFileName(path));
    }
}
