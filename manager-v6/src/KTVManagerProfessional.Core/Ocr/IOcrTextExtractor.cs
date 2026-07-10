namespace KTVManagerProfessional.Core.Ocr;

public interface IOcrTextExtractor
{
    bool IsAvailable { get; }

    string AvailabilityMessage { get; }

    Task<string> ExtractTextAsync(string imagePath, CancellationToken cancellationToken);
}
