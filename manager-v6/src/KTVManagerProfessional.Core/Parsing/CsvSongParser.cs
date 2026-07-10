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

        return TabularSongParser.Parse(rows[0], rows.Skip(1).ToList(), brandCode, Path.GetFileName(path));
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
