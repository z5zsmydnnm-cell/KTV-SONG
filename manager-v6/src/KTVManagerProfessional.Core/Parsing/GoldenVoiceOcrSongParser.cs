using System.Text;
using System.Text.RegularExpressions;
using KTVManagerProfessional.Core.Importing;
using KTVManagerProfessional.Core.Ocr;

namespace KTVManagerProfessional.Core.Parsing;

public sealed partial class GoldenVoiceOcrSongParser
{
    public ParseResult ParsePages(IReadOnlyList<OcrPage> pages, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(pages);

        var songs = new List<SongRecord>();
        var issues = new List<ParseIssue>();
        var volume = DetectVolume(sourceName);

        foreach (var page in pages)
        {
            var language = DetectLanguage(page);
            foreach (var row in FindRows(page))
            {
                var title = NormalizeTitle(row.TitlePrefix + JoinWords(FindTitleWords(page, row)));
                var artist = JoinWords(FindArtistWords(page, row));

                if (string.IsNullOrWhiteSpace(title) || !HasUsefulTitleText(title))
                {
                    issues.Add(new ParseIssue(page.PageNumber, row.SongNumber, "OCR row is missing title."));
                    continue;
                }

                songs.Add(new SongRecord(
                    SongNumber: row.SongNumber,
                    Title: title,
                    Artist: artist,
                    Language: language,
                    BrandCode: BrandCode.GoldenVoice,
                    Volume: volume));
            }
        }

        var ordered = songs
            .GroupBy(song => song.SongNumber, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(song => song.SongNumber, StringComparer.Ordinal)
            .ToList();

        if (ordered.Count == 0 && issues.Count == 0)
        {
            issues.Add(new ParseIssue(0, sourceName, "No song rows matched the GoldenVoice OCR parser."));
        }

        return new ParseResult(ordered, issues);
    }

    private static IReadOnlyList<OcrRow> FindRows(OcrPage page)
    {
        var rows = new List<OcrRow>();
        rows.AddRange(FindRowsInColumn(page, isLeftColumn: true));
        rows.AddRange(FindRowsInColumn(page, isLeftColumn: false));

        return rows
            .OrderBy(row => row.Y)
            .ThenBy(row => row.IsLeftColumn ? 0 : 1)
            .ToList();
    }

    private static IReadOnlyList<OcrRow> FindRowsInColumn(OcrPage page, bool isLeftColumn)
    {
        var numberWords = page.Words
            .Where(word => word.CenterY > page.Height * 0.07)
            .Where(word => IsInNumberBand(page, word, isLeftColumn))
            .Where(word => DigitRegex().IsMatch(word.Text))
            .OrderBy(word => word.CenterY)
            .ThenBy(word => word.X)
            .ToList();

        var groups = new List<List<OcrWord>>();
        foreach (var word in numberWords)
        {
            var group = groups.LastOrDefault();
            if (group is not null && Math.Abs(group.Average(item => item.CenterY) - word.CenterY) <= 24)
            {
                group.Add(word);
            }
            else
            {
                groups.Add([word]);
            }
        }

        var rows = new List<OcrRow>();
        foreach (var group in groups)
        {
            var orderedGroup = group.OrderBy(word => word.X).ToList();
            var mergedText = string.Concat(orderedGroup.Select(word => word.Text.Trim()));
            var mergedSongTitle = MergedSongNumberTitleRegex().Match(mergedText);
            var digits = mergedSongTitle.Success
                ? mergedSongTitle.Groups["number"].Value
                : string.Concat(orderedGroup.Select(word => NonDigitRegex().Replace(word.Text, string.Empty)));

            digits = NormalizeSongNumber(digits);
            if (!SongNumberRegex().IsMatch(digits))
            {
                continue;
            }

            var titlePrefix = mergedSongTitle.Success ? mergedSongTitle.Groups["title"].Value : string.Empty;
            rows.Add(new OcrRow(digits, group.Average(word => word.CenterY), isLeftColumn, titlePrefix));
        }

        var orderedRows = rows.OrderBy(row => row.Y).ToList();
        for (var index = 0; index < orderedRows.Count; index++)
        {
            var nextY = index + 1 < orderedRows.Count ? orderedRows[index + 1].Y : page.Height;
            orderedRows[index] = orderedRows[index] with { NextY = nextY };
        }

        return orderedRows;
    }

    private static IReadOnlyList<OcrWord> FindTitleWords(OcrPage page, OcrRow row)
    {
        var words = page.Words
            .Where(word => IsInTitleBand(page, word, row.IsLeftColumn))
            .Where(word => Math.Abs(word.CenterY - row.Y) <= 35)
            .ToList();

        return OrderWordsByReadingLine(DeduplicateOcrAlternatives(words));
    }

    private static IReadOnlyList<OcrWord> FindArtistWords(OcrPage page, OcrRow row)
    {
        var lowerY = row.Y - 45;
        var upperY = Math.Min(row.NextY - 20, row.Y + 36);
        var words = page.Words
            .Where(word => IsInArtistBand(page, word, row.IsLeftColumn))
            .Where(word => word.CenterY >= lowerY && word.CenterY <= upperY)
            .ToList();

        return OrderWordsByReadingLine(DeduplicateOcrAlternatives(words));
    }

    private static IReadOnlyList<OcrWord> DeduplicateOcrAlternatives(IReadOnlyList<OcrWord> words)
    {
        var clusters = new List<List<(OcrWord Word, int Index)>>();
        for (var index = 0; index < words.Count; index++)
        {
            var word = words[index];
            var cluster = clusters.FirstOrDefault(items => items.Any(item => IsOverlappingAlternative(item.Word, word)));
            if (cluster is null)
            {
                clusters.Add([(word, index)]);
            }
            else
            {
                cluster.Add((word, index));
            }
        }

        return clusters
            .Select(cluster => cluster
                .OrderByDescending(item => ScoreOcrAlternative(item.Word.Text))
                .ThenByDescending(item => item.Index)
                .First()
                .Word)
            .ToList();
    }

    private static bool IsOverlappingAlternative(OcrWord left, OcrWord right)
    {
        return Math.Abs(left.CenterY - right.CenterY) <= 24 &&
            Math.Abs(left.CenterX - right.CenterX) <= 24;
    }

    private static int ScoreOcrAlternative(string text)
    {
        if (text.Any(IsKana))
        {
            return 10;
        }

        if (text.All(char.IsPunctuation) || text.All(char.IsSymbol))
        {
            return 0;
        }

        if (text.Any(IsCjkUnifiedIdeograph))
        {
            return 100;
        }

        if (text.Any(char.IsLetterOrDigit))
        {
            return 80;
        }

        return 20;
    }

    private static bool IsKana(char character)
    {
        return character is >= '\u3040' and <= '\u30ff';
    }

    private static bool IsCjkUnifiedIdeograph(char character)
    {
        return character is >= '\u4e00' and <= '\u9fff';
    }

    private static bool HasUsefulTitleText(string title)
    {
        return title.Any(character =>
            char.IsLetter(character) ||
            IsKana(character) ||
            IsCjkUnifiedIdeograph(character));
    }

    private static IReadOnlyList<OcrWord> OrderWordsByReadingLine(IReadOnlyList<OcrWord> words)
    {
        var lines = new List<List<OcrWord>>();
        foreach (var word in words.OrderBy(word => word.CenterY).ThenBy(word => word.X))
        {
            var line = lines.LastOrDefault();
            if (line is not null && Math.Abs(line.Average(item => item.CenterY) - word.CenterY) <= 24)
            {
                line.Add(word);
            }
            else
            {
                lines.Add([word]);
            }
        }

        return lines
            .OrderBy(line => line.Average(word => word.CenterY))
            .SelectMany(line => line.OrderBy(word => word.X))
            .ToList();
    }

    private static bool IsInNumberBand(OcrPage page, OcrWord word, bool isLeftColumn)
    {
        var width = page.Width;
        return isLeftColumn
            ? word.CenterX >= width * 0.07 && word.CenterX <= width * 0.18
            : word.CenterX >= width * 0.53 && word.CenterX <= width * 0.60;
    }

    private static bool IsInTitleBand(OcrPage page, OcrWord word, bool isLeftColumn)
    {
        var width = page.Width;
        return isLeftColumn
            ? word.CenterX >= width * 0.17 && word.CenterX <= width * 0.43
            : word.CenterX >= width * 0.61 && word.CenterX <= width * 0.85;
    }

    private static bool IsInArtistBand(OcrPage page, OcrWord word, bool isLeftColumn)
    {
        var width = page.Width;
        return isLeftColumn
            ? word.CenterX >= width * 0.43 && word.CenterX <= width * 0.50
            : word.CenterX >= width * 0.88 && word.CenterX <= width * 0.99;
    }

    private static string DetectLanguage(OcrPage page)
    {
        var text = JoinWords(page.Words);
        if (text.Contains("\u65e5\u8a9e", StringComparison.Ordinal))
        {
            return "\u65e5\u8a9e";
        }

        foreach (var language in new[] { "台語", "國語", "華語", "客語" })
        {
            if (text.Contains(language, StringComparison.Ordinal))
            {
                return language;
            }
        }

        return "Unknown";
    }

    private static string JoinWords(IEnumerable<OcrWord> words)
    {
        var builder = new StringBuilder();
        foreach (var word in words)
        {
            var text = word.Text.Trim();
            if (text.Length == 0)
            {
                continue;
            }

            builder.Append(text);
        }

        return builder.ToString();
    }

    private static string NormalizeTitle(string title)
    {
        var normalized = title
            .Replace('\u2160', 'I')
            .Replace('\u00b7', '.');

        while (normalized.Length > 1 &&
            (char.IsDigit(normalized[0]) || char.IsPunctuation(normalized[0]) || char.IsSymbol(normalized[0])) &&
            IsCjkUnifiedIdeograph(normalized[1]))
        {
            normalized = normalized[1..];
        }

        if (!normalized.Contains('.', StringComparison.Ordinal))
        {
            return normalized;
        }

        var parts = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || parts.Any(part => part.Length != 1 || !"10IO".Contains(part, StringComparison.OrdinalIgnoreCase)))
        {
            return normalized;
        }

        return string.Join(".", parts.Select(part => part switch
        {
            "1" => "I",
            "0" => "O",
            _ => part.ToUpperInvariant()
        }));
    }

    private static string NormalizeSongNumber(string digits)
    {
        return digits.Length == 6 ? digits[..5] : digits;
    }

    private static string DetectVolume(string sourceName)
    {
        var match = VolumeRegex().Match(sourceName ?? string.Empty);
        return match.Success ? match.Groups["volume"].Value : string.Empty;
    }

    private sealed record OcrRow(string SongNumber, double Y, bool IsLeftColumn, string TitlePrefix)
    {
        public double NextY { get; init; } = double.MaxValue;
    }

    [GeneratedRegex(@"\d", RegexOptions.Compiled)]
    private static partial Regex DigitRegex();

    [GeneratedRegex(@"\D", RegexOptions.Compiled)]
    private static partial Regex NonDigitRegex();

    [GeneratedRegex(@"^\d{5}$", RegexOptions.Compiled)]
    private static partial Regex SongNumberRegex();

    [GeneratedRegex(@"^(?<number>\d{5})(?<title>\D.+)$", RegexOptions.Compiled)]
    private static partial Regex MergedSongNumberTitleRegex();

    [GeneratedRegex(@"(?<!\d)(?<volume>\d{1,4})(?!\d)", RegexOptions.Compiled)]
    private static partial Regex VolumeRegex();
}
