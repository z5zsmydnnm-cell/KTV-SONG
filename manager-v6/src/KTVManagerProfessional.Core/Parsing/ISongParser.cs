namespace KTVManagerProfessional.Core.Parsing;

public interface ISongParser
{
    ParseResult Parse(string text, string sourceName);
}
