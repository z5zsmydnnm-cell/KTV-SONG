namespace KTVManagerProfessional.Core.Ocr;

public sealed class UnavailableOcrTextExtractor : IOcrTextExtractor
{
    public bool IsAvailable => false;

    public string AvailabilityMessage => "OCR engine is not configured on this Windows installation.";

    public Task<string> ExtractTextAsync(string imagePath, CancellationToken cancellationToken)
    {
        return Task.FromException<string>(new InvalidOperationException(AvailabilityMessage));
    }
}
