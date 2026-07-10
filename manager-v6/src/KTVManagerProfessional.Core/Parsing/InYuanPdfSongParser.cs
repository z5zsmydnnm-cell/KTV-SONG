namespace KTVManagerProfessional.Core.Parsing;

public sealed class InYuanPdfSongParser : ISongParser
{
    public ParseResult Parse(string text, string sourceName)
    {
        return InYuanSongParser.ParseText(text, sourceName);
    }
}
