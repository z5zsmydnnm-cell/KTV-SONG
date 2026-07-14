using System.Text.RegularExpressions;

namespace KTVManagerProfessional.Core;

public static partial class InYuanSongParser
{
    private const string BrandCode = "音圓";

    private static readonly string[] NoiseWords =
    [
        "勃露斯",
        "吉魯巴",
        "Shuffle",
        "R&B",
        "音圓唱片",
        "歌曲類型說明"
    ];

    private static readonly string[] OldCatalogRhythmSuffixes =
    [
        "Slow Disco",
        "Slow Rock",
        "Slow Soul",
        "Hip-Hop",
        "Cha Cha",
        "Tango",
        "勃露斯",
        "吉魯巴",
        "迪斯可",
        "華爾滋",
        "恰恰",
        "倫巴",
        "探戈",
        "梭",
        "扭扭",
        "勃露斯",
        "吉魯巴",
        "梭",
        "Samba",
        "Soul",
        "Waltz",
        "Trot",
        "Rock",
        "Disco",
        "R&B",
        "Shuffle"
    ];

    public static ParseResult ParseText(string text, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(text);

        var volume = DetectVolume(text, sourceName);
        var songs = new List<SongRecord>();
        var issues = new List<ParseIssue>();
        var currentLanguage = string.Empty;
        var lines = ToLines(text);
        var oldCatalogBlocks = ParseOldCatalogBlocks(lines, volume);
        songs.AddRange(oldCatalogBlocks.Songs);

        for (var index = 0; index < lines.Count; index++)
        {
            if (oldCatalogBlocks.ConsumedLineNumbers.Contains(lines[index].Number))
            {
                continue;
            }

            var line = lines[index].Text;

            if (TryDetectLanguage(line, out var language))
            {
                currentLanguage = language;
                continue;
            }

            if (ShouldIgnore(line) || IsDocumentHeading(line))
            {
                continue;
            }

            if (TryParseOldCatalogRow(line, currentLanguage, volume, out var oldCatalogSong))
            {
                songs.Add(oldCatalogSong);
                continue;
            }

            var songChunks = SplitSongChunks(line);
            if (songChunks.Count == 0)
            {
                continue;
            }

            if (songChunks.Count > 1)
            {
                foreach (var chunk in songChunks)
                {
                    AddParsedSong(chunk, lines[index].Number, line, currentLanguage, volume, allowMissingArtist: true, songs, issues);
                }

                continue;
            }

            var match = SongLineRegex().Match(songChunks[0]);
            var songNumber = match.Groups["number"].Value;
            var firstSegment = match.Groups["rest"].Value.Trim();
            var parts = new SongParts();
            AddSegment(parts, firstSegment);

            while (index + 1 < lines.Count)
            {
                var next = lines[index + 1].Text;
                if (SongLineRegex().IsMatch(next) || TryDetectLanguage(next, out _) || ShouldIgnore(next) || IsDocumentHeading(next))
                {
                    break;
                }

                index++;
                AddSegment(parts, next);
            }

            var title = Join(parts.TitleParts);
            var artist = Join(parts.ArtistParts);
            if (title.Length == 0)
            {
                issues.Add(new ParseIssue(lines[index].Number, line, "Song line is missing title or artist."));
                continue;
            }

            songs.Add(new SongRecord(songNumber, title, artist, currentLanguage, BrandCode, volume));
        }

        var dedupedSongs = songs
            .GroupBy(song => song.SongNumber, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(song => song.SongNumber, StringComparer.Ordinal)
            .ToList();

        if (dedupedSongs.Count == 0 && issues.Count == 0)
        {
            issues.Add(new ParseIssue(0, sourceName, "No song rows matched the InYuan PDF parser. The PDF may be scanned/image-only, have no text layer, or use an unsupported layout."));
        }

        return new ParseResult(dedupedSongs, issues);
    }

    private static IReadOnlyList<string> SplitSongChunks(string line)
    {
        var matches = SongStartRegex().Matches(line);
        if (matches.Count == 0)
        {
            return [];
        }

        var chunks = new List<string>(matches.Count);
        for (var index = 0; index < matches.Count; index++)
        {
            var start = matches[index].Index;
            var end = index + 1 < matches.Count ? matches[index + 1].Index : line.Length;
            var chunk = line[start..end].Trim();
            if (chunk.Length > 0)
            {
                chunks.Add(chunk);
            }
        }

        return chunks;
    }

    private static void AddParsedSong(
        string chunk,
        int lineNumber,
        string originalLine,
        string currentLanguage,
        string volume,
        bool allowMissingArtist,
        List<SongRecord> songs,
        List<ParseIssue> issues)
    {
        var match = SongLineRegex().Match(chunk);
        if (!match.Success)
        {
            issues.Add(new ParseIssue(lineNumber, originalLine, "Song line cannot be split into song number and title."));
            return;
        }

        var songNumber = match.Groups["number"].Value;
        var rest = match.Groups["rest"].Value.Trim();
        if (rest.Length == 0)
        {
            issues.Add(new ParseIssue(lineNumber, originalLine, "Song line is missing title or artist."));
            return;
        }

        var parts = new SongParts();
        AddSegment(parts, rest);
        var title = Join(parts.TitleParts);
        var artist = Join(parts.ArtistParts);

        if (title.Length == 0 || (!allowMissingArtist && artist.Length == 0))
        {
            issues.Add(new ParseIssue(lineNumber, originalLine, "Song line is missing title or artist."));
            return;
        }

        songs.Add(new SongRecord(songNumber, title, artist, currentLanguage, BrandCode, volume));
    }

    private static IReadOnlyList<(int Number, string Text)> ToLines(string text)
    {
        return text
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split('\n')
            .Select((line, index) => (Number: index + 1, Text: Normalize(line)))
            .Where(line => line.Text.Length > 0)
            .ToList();
    }

    private static string Normalize(string line)
    {
        return line
            .Replace('\u00a0', ' ')
            .Replace('\u3000', ' ')
            .Replace("\t", "  ", StringComparison.Ordinal)
            .Trim();
    }

    private static bool ShouldIgnore(string line)
    {
        return NoiseWords.Any(word => line.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDocumentHeading(string line)
    {
        return line.Contains("音圓", StringComparison.Ordinal) || line.Contains("第", StringComparison.Ordinal) && VolumeRegex().IsMatch(line);
    }

    private static bool TryDetectLanguage(string line, out string language)
    {
        foreach (var candidate in new[] { "台語", "國語", "華語", "客語" })
        {
            if (line.Contains(candidate, StringComparison.Ordinal))
            {
                language = candidate;
                return true;
            }
        }

        language = string.Empty;
        return false;
    }

    private static string DetectVolume(string text, string sourceName)
    {
        var sourceMatch = VolumeRegex().Match(sourceName ?? string.Empty);
        if (sourceMatch.Success)
        {
            return sourceMatch.Groups["volume"].Value;
        }

        var textMatch = VolumeRegex().Match(text);
        return textMatch.Success ? textMatch.Groups["volume"].Value : string.Empty;
    }

    private static bool TryParseOldCatalogRow(string line, string currentLanguage, string volume, out SongRecord song)
    {
        song = default!;

        var match = OldCatalogLineRegex().Match(line);
        if (!match.Success)
        {
            return false;
        }

        var partsMatch = OldCatalogPartsRegex().Match(match.Groups["rest"].Value.Trim());
        if (!partsMatch.Success)
        {
            return false;
        }

        var title = partsMatch.Groups["title"].Value.Trim();
        var artist = StripOldCatalogRhythm(partsMatch.Groups["artist"].Value);
        if (title.Length == 0 || artist.Length == 0)
        {
            return false;
        }

        song = new SongRecord(
            match.Groups["number"].Value,
            title,
            artist,
            NormalizeOldCatalogLanguage(partsMatch.Groups["language"].Value, currentLanguage),
            BrandCode,
            volume);
        return true;
    }

    private static OldCatalogBlockParseResult ParseOldCatalogBlocks(IReadOnlyList<(int Number, string Text)> lines, string volume)
    {
        var songs = new List<SongRecord>();
        var consumedLineNumbers = new HashSet<int>();
        var block = new List<(int Number, string Text)>();
        var inBlock = false;
        var pendingLanguage = string.Empty;
        var blockLanguage = string.Empty;

        foreach (var line in lines)
        {
            if (TryDetectOldCatalogLanguageLine(line.Text, out var detectedLanguage))
            {
                pendingLanguage = detectedLanguage;
                if (inBlock)
                {
                    block.Add(line);
                }

                continue;
            }

            if (TryStartOldCatalogBlock(line.Text, out var rest))
            {
                AddOldCatalogBlock(block, volume, blockLanguage, songs, consumedLineNumbers);
                block.Clear();
                inBlock = true;
                blockLanguage = pendingLanguage;
                block.Add((line.Number, string.Empty));
                if (TryDetectOldCatalogLanguageLine(rest, out var markerLanguage))
                {
                    blockLanguage = markerLanguage;
                }
                else if (rest.Length > 0)
                {
                    block[^1] = (line.Number, rest);
                }

                continue;
            }

            if (inBlock)
            {
                block.Add(line);
            }
        }

        AddOldCatalogBlock(block, volume, blockLanguage, songs, consumedLineNumbers);
        return new OldCatalogBlockParseResult(songs, consumedLineNumbers);
    }

    private static void AddOldCatalogBlock(
        List<(int Number, string Text)> block,
        string volume,
        string language,
        List<SongRecord> songs,
        HashSet<int> consumedLineNumbers)
    {
        if (TryParseOldCatalogBlock(block.Select(line => line.Text).ToList(), volume, language, out var song))
        {
            songs.Add(song);
            foreach (var lineNumber in block.Select(line => line.Number))
            {
                consumedLineNumbers.Add(lineNumber);
            }
        }
    }

    private static bool TryParseOldCatalogBlock(IReadOnlyList<string> block, string volume, string fallbackLanguage, out SongRecord song)
    {
        song = default!;
        if (block.Count == 0)
        {
            return false;
        }

        var lines = block
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !IsOldCatalogFooter(line))
            .ToList();
        var language = fallbackLanguage;
        for (var index = 0; index < lines.Count; index++)
        {
            if (TryDetectOldCatalogLanguageLine(lines[index], out var detectedLanguage))
            {
                language = detectedLanguage;
                lines[index] = string.Empty;
            }
        }

        var number = string.Empty;
        var numberLineIndex = -1;
        var numberLineRemainder = string.Empty;
        for (var index = 0; index < lines.Count; index++)
        {
            var numberMatch = OldCatalogNumberTokenRegex().Match(lines[index]);
            if (!numberMatch.Success)
            {
                continue;
            }

            number = numberMatch.Groups["number"].Value;
            lines[index] = OldCatalogNumberTokenRegex().Replace(lines[index], string.Empty, 1).Trim();
            numberLineIndex = index;
            numberLineRemainder = lines[index];
            break;
        }

        if (number.Length == 0)
        {
            return false;
        }

        for (var index = 0; index < lines.Count; index++)
        {
            var titleLanguageMatch = OldCatalogTitleLanguageRegex().Match(lines[index]);
            if (!titleLanguageMatch.Success)
            {
                continue;
            }

            var title = titleLanguageMatch.Groups["title"].Value.Trim();
            var artist = titleLanguageMatch.Groups["artist"].Value.Trim();
            if (artist.Length == 0 && index > 0)
            {
                artist = string.Join(' ', lines.Take(index));
            }

            artist = StripOldCatalogRhythm(artist);
            if (title.Length == 0 || artist.Length == 0)
            {
                return false;
            }

            song = new SongRecord(
                number,
                title,
                artist,
                NormalizeOldCatalogLanguage(titleLanguageMatch.Groups["language"].Value, string.Empty),
                BrandCode,
                volume);
            return true;
        }

        return language.Length > 0 &&
            TryParseOldCatalogBlockWithoutLanguage(lines, number, numberLineIndex, numberLineRemainder, language, volume, out song);
    }

    private static bool TryStartOldCatalogBlock(string line, out string rest)
    {
        var value = line.Trim();
        if (value.Length == 0 || !IsOldCatalogMarker(value[0]))
        {
            rest = string.Empty;
            return false;
        }

        rest = value[1..].Trim();
        return true;
    }

    private static bool IsOldCatalogMarker(char value)
    {
        return value is '\u25cf' or '\u25cb' or '\u2605' or '\u25c6' or '\u25c7' or '\u0095' or '\u009b' or '*';
    }

    private static bool TryParseOldCatalogBlockWithoutLanguage(
        IReadOnlyList<string> lines,
        string number,
        int numberLineIndex,
        string numberLineRemainder,
        string language,
        string volume,
        out SongRecord song)
    {
        song = default!;
        var title = string.Empty;
        var artist = string.Empty;

        if (numberLineRemainder.Length > 0 && numberLineIndex > 0)
        {
            title = numberLineRemainder;
            artist = StripOldCatalogRhythm(string.Join(' ', lines.Take(numberLineIndex).Where(line => line.Length > 0)));
        }
        else
        {
            var content = lines.Where(line => line.Length > 0).ToList();
            if (content.Count >= 2 && EndsWithOldCatalogRhythm(content[0]))
            {
                artist = StripOldCatalogRhythm(content[0]);
                title = content[1].Trim();
            }
            else if (content.Count == 1)
            {
                TrySplitOldCatalogTitleArtistLine(content[0], out title, out artist);
            }
        }

        if (title.Length == 0 || artist.Length == 0)
        {
            return false;
        }

        song = new SongRecord(number, title, artist, language, BrandCode, volume);
        return true;
    }

    private static bool TrySplitOldCatalogTitleArtistLine(string line, out string title, out string artist)
    {
        var value = StripOldCatalogRhythm(line);
        var delimiter = value.LastIndexOf(' ');
        if (delimiter <= 0 || delimiter >= value.Length - 1)
        {
            title = string.Empty;
            artist = string.Empty;
            return false;
        }

        title = value[..delimiter].Trim();
        artist = value[(delimiter + 1)..].Trim();
        return title.Length > 0 && artist.Length > 0;
    }

    private static bool EndsWithOldCatalogRhythm(string value)
    {
        return OldCatalogRhythmSuffixes.Any(suffix => value.EndsWith(" " + suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryDetectOldCatalogLanguageLine(string line, out string language)
    {
        language = NormalizeOldCatalogLanguage(line.Trim(), string.Empty);
        return language.Length > 0;
    }

    private static bool IsOldCatalogFooter(string line)
    {
        return line.Contains("代表", StringComparison.Ordinal) ||
            line.Contains("合法", StringComparison.Ordinal) ||
            line.Contains("專輯", StringComparison.Ordinal) ||
            line.Contains("MIDI", StringComparison.Ordinal) ||
            line.Contains("貼紙", StringComparison.Ordinal) ||
            line.Contains("Singing", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("歌 號", StringComparison.Ordinal) ||
            line.Contains("有無", StringComparison.Ordinal) ||
            line.Equals("人聲", StringComparison.Ordinal);
    }

    private static string NormalizeOldCatalogLanguage(string language, string fallback)
    {
        if (language is "\u53f0\u8a9e" or "\u570b\u8a9e" or "\u83ef\u8a9e" or "\u5ba2\u8a9e")
        {
            return language;
        }

        return language switch
        {
            "台" => "台語",
            "國" or "国" => "國語",
            "華" or "华" => "華語",
            "客" => "客語",
            _ => fallback
        };
    }

    private static string StripOldCatalogRhythm(string artist)
    {
        var value = artist.Trim();
        foreach (var suffix in OldCatalogRhythmSuffixes)
        {
            if (value.EndsWith(" " + suffix, StringComparison.OrdinalIgnoreCase))
            {
                return value[..^suffix.Length].Trim();
            }
        }

        return value;
    }

    private static void AddSegment(SongParts parts, string segment)
    {
        var delimiter = DoubleSpaceRegex().Match(segment);
        if (delimiter.Success)
        {
            var titlePart = segment[..delimiter.Index].Trim();
            var artistPart = segment[(delimiter.Index + delimiter.Length)..].Trim();
            if (titlePart.Length > 0)
            {
                parts.TitleParts.Add(titlePart);
            }

            if (artistPart.Length > 0)
            {
                parts.ArtistParts.Add(artistPart);
            }

            return;
        }

        if (parts.ArtistParts.Count > 0)
        {
            parts.ArtistParts.Add(segment);
        }
        else
        {
            parts.TitleParts.Add(segment);
        }
    }

    private static string Join(IEnumerable<string> parts)
    {
        return string.Join(' ', parts.Select(part => SingleSpaceRegex().Replace(part, " ").Trim()).Where(part => part.Length > 0));
    }

    private sealed class SongParts
    {
        public List<string> TitleParts { get; } = [];

        public List<string> ArtistParts { get; } = [];
    }

    private sealed record OldCatalogBlockParseResult(IReadOnlyList<SongRecord> Songs, HashSet<int> ConsumedLineNumbers);

    [GeneratedRegex(@"(?<!\d)(?<volume>\d{4})(?!\d)", RegexOptions.Compiled)]
    private static partial Regex VolumeRegex();

    [GeneratedRegex(@"^(?:[◆◇★☆●◎■□*]+\s*)?(?<number>\d{5,6})(?:\s+(?<rest>.*))?$", RegexOptions.Compiled)]
    private static partial Regex SongLineRegex();

    [GeneratedRegex(@"(?<!\d)(?<number>\d{5,6})(?!\d)", RegexOptions.Compiled)]
    private static partial Regex SongStartRegex();

    [GeneratedRegex(@"^(?:[\u25cf\u25cb\u2605\u25c6\u25c7*]\s*)?(?<number>\d{4,6})\s+(?<rest>.+)$", RegexOptions.Compiled)]
    private static partial Regex OldCatalogLineRegex();

    [GeneratedRegex(@"^(?<title>.+?)\s+(?<language>台|國|国|華|华|客)\s+(?<artist>.+)$", RegexOptions.Compiled)]
    private static partial Regex OldCatalogPartsRegex();

    [GeneratedRegex(@"(?<!\d)(?<number>\d{4,6})(?!\d)", RegexOptions.Compiled)]
    private static partial Regex OldCatalogNumberTokenRegex();

    [GeneratedRegex(@"^(?<title>.+?)\s+(?<language>\u53f0\u8a9e|\u570b\u8a9e|\u83ef\u8a9e|\u5ba2\u8a9e|\u53f0|\u570b|\u56fd|\u83ef|\u534e|\u5ba2)(?:\s+(?<artist>.*))?$", RegexOptions.Compiled)]
    private static partial Regex OldCatalogTitleLanguageRegex();

    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
    private static partial Regex DoubleSpaceRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex SingleSpaceRegex();
}
