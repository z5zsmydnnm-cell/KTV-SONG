namespace KTVManagerProfessional.Core;

public sealed record ParseIssue(int LineNumber, string Line, string Reason);
