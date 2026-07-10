using KTVManagerProfessional.Core.Importing;

namespace KTVManagerProfessional.Core.Parsing;

public static class TabularSongParser
{
    public static ParseResult Parse(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        string brandCode,
        string sourceName)
    {
        var map = ColumnRecognizer.Recognize(headers, rows.Take(20).ToList());
        var songs = new List<SongRecord>();
        var issues = new List<ParseIssue>();

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var lineNumber = index + 2;
            var songNumber = Get(row, map.SongNumberIndex);
            var title = Get(row, map.TitleIndex);
            var artist = Get(row, map.ArtistIndex);
            var language = Get(row, map.LanguageIndex);
            var volume = Get(row, map.VolumeIndex);
            var rowBrand = Get(row, map.BrandIndex);

            if (string.IsNullOrWhiteSpace(songNumber) && string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(songNumber) || string.IsNullOrWhiteSpace(title))
            {
                issues.Add(new ParseIssue(lineNumber, string.Join('|', row), "Tabular row is missing song number or title."));
                continue;
            }

            songs.Add(new SongRecord(
                SongNumber: songNumber,
                Title: title,
                Artist: artist,
                Language: string.IsNullOrWhiteSpace(language) ? "Unknown" : language,
                BrandCode: string.IsNullOrWhiteSpace(rowBrand) ? brandCode : rowBrand,
                Volume: volume));
        }

        return new ParseResult(songs, issues);
    }

    private static string Get(IReadOnlyList<string> row, int? index)
    {
        if (index is null || index.Value < 0 || index.Value >= row.Count)
        {
            return string.Empty;
        }

        return row[index.Value].Trim();
    }
}
