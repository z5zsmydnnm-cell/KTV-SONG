namespace KTVManagerProfessional.Core.Importing;

public sealed record ImportRoute(
    string SourcePath,
    ImportSourceType SourceType,
    string BrandCode,
    bool IsUnsupported,
    string Reason);
