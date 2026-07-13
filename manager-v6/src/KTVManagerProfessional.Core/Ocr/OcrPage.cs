namespace KTVManagerProfessional.Core.Ocr;

public sealed record OcrPage(
    int PageNumber,
    double Width,
    double Height,
    IReadOnlyList<OcrWord> Words);
