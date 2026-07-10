namespace KTVManagerProfessional.Core.Preview;

public sealed record PdfPageDiagnostic(
    long ImportHistoryId,
    int PageNumber,
    int TextLayerCharacterCount,
    int SongLikeRowCount,
    int ParserIssueCount,
    bool OcrRan,
    int OcrCharacterCount,
    double Confidence,
    DateTimeOffset CreatedAt);
