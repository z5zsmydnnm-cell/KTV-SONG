namespace KTVManagerProfessional.Core;

public sealed record ParseResult(IReadOnlyList<SongRecord> Songs, IReadOnlyList<ParseIssue> Issues);
