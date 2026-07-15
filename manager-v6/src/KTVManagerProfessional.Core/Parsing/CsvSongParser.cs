using System.Text;
using KTVManagerProfessional.Core.Importing;

namespace KTVManagerProfessional.Core.Parsing;

public sealed class CsvSongParser
{
    public ParseResult ParseFile(string path, string brandCode)
    {
        var rows = File.ReadAllLines(path, Encoding.UTF8).Select(ParseCsvLine).ToList();
        if (rows.Count == 0)
        {
            return new ParseResult([], [new ParseIssue(0, string.Empty, "CSV is empty.")]);
        }

        if (TryParseMultiBrandCodeRows(rows[0], rows.Skip(1).ToList(), out var multiBrandResult))
        {
            return multiBrandResult;
        }

        return TabularSongParser.Parse(rows[0], rows.Skip(1).ToList(), brandCode, Path.GetFileName(path));
    }

    private static bool TryParseMultiBrandCodeRows(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        out ParseResult result)
    {
        var titleIndex = FindHeader(headers, "歌名");
        var artistIndex = FindHeader(headers, "歌手");
        var languageIndex = FindHeader(headers, "語言");
        var volumeIndex = FindHeader(headers, "集數");
        var inYuanIndex = FindHeader(headers, "音圓代號");
        var goldenVoiceIndex = FindHeader(headers, "金嗓代號");
        var hongYinIndex = FindHeader(headers, "弘音代號");

        if (titleIndex is null || (inYuanIndex is null && goldenVoiceIndex is null && hongYinIndex is null))
        {
            result = new ParseResult([], []);
            return false;
        }

        var songs = new List<SongRecord>();
        var issues = new List<ParseIssue>();
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var title = Get(row, titleIndex);
            var artist = Get(row, artistIndex);
            var language = Get(row, languageIndex);
            var volume = Get(row, volumeIndex);
            var codes = new[]
            {
                (Code: Get(row, inYuanIndex), Brand: BrandCode.InYuan),
                (Code: Get(row, goldenVoiceIndex), Brand: BrandCode.GoldenVoice),
                (Code: Get(row, hongYinIndex), Brand: "弘音")
            }.Where(item => !string.IsNullOrWhiteSpace(item.Code)).ToList();

            if (codes.Count == 0)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                issues.Add(new ParseIssue(index + 2, string.Join('|', row), "Tabular row is missing title."));
                continue;
            }

            foreach (var code in codes)
            {
                songs.Add(new SongRecord(
                    SongNumber: code.Code,
                    Title: title,
                    Artist: artist,
                    Language: string.IsNullOrWhiteSpace(language) ? "Unknown" : language,
                    BrandCode: code.Brand,
                    Volume: volume));
            }
        }

        result = new ParseResult(songs, issues);
        return true;
    }

    private static int? FindHeader(IReadOnlyList<string> headers, string name)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            if (string.Equals(NormalizeHeader(headers[index]), name, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return null;
    }

    private static string NormalizeHeader(string header)
    {
        return header.Trim().TrimStart('\uFEFF');
    }

    private static string Get(IReadOnlyList<string> row, int? index)
    {
        if (index is null || index.Value < 0 || index.Value >= row.Count)
        {
            return string.Empty;
        }

        return row[index.Value].Trim();
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var builder = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var ch = line[index];
            if (ch == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    builder.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                values.Add(builder.ToString());
                builder.Clear();
            }
            else
            {
                builder.Append(ch);
            }
        }

        values.Add(builder.ToString());
        return values;
    }
}
