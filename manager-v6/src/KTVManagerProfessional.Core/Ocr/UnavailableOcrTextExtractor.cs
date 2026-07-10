namespace KTVManagerProfessional.Core.Ocr;

public sealed class UnavailableOcrTextExtractor : IOcrTextExtractor
{
    public bool IsAvailable => false;

    public string AvailabilityMessage => "這台 Windows 尚未設定 OCR 引擎。";

    public Task<string> ExtractTextAsync(string imagePath, CancellationToken cancellationToken)
    {
        return Task.FromException<string>(new InvalidOperationException(AvailabilityMessage));
    }
}
