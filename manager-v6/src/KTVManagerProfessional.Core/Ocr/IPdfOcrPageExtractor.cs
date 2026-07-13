namespace KTVManagerProfessional.Core.Ocr;

public interface IPdfOcrPageExtractor
{
    bool IsAvailable { get; }

    string AvailabilityMessage { get; }

    Task<IReadOnlyList<OcrPage>> ExtractPagesAsync(string pdfPath, CancellationToken cancellationToken);
}
