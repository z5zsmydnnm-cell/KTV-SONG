namespace KTVManagerProfessional.Core.Corrections;

public sealed record ManualCorrection(
    long ImportHistoryId,
    int PageNumber,
    string RawText,
    string? OcrText,
    string? SongNumber,
    string? Title,
    string? ArtistName,
    string? Language,
    string? BrandCode,
    string? VolumeCode);
