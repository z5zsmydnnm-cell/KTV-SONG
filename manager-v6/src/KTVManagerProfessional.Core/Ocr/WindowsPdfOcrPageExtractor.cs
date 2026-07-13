using Windows.Data.Pdf;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

namespace KTVManagerProfessional.Core.Ocr;

public sealed class WindowsPdfOcrPageExtractor : IPdfOcrPageExtractor
{
    private const double RenderScale = 2.5;

    public bool IsAvailable => OperatingSystem.IsWindows() && CreateEngine() is not null;

    public string AvailabilityMessage => IsAvailable
        ? "Windows OCR is available."
        : "Windows OCR is not available. Install Traditional Chinese OCR language support in Windows settings.";

    public async Task<IReadOnlyList<OcrPage>> ExtractPagesAsync(string pdfPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);
        cancellationToken.ThrowIfCancellationRequested();

        var engine = CreateEngine() ?? throw new InvalidOperationException(AvailabilityMessage);
        var file = await StorageFile.GetFileFromPathAsync(pdfPath);
        var document = await PdfDocument.LoadFromFileAsync(file);
        var pages = new List<OcrPage>();

        for (uint index = 0; index < document.PageCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var page = document.GetPage(index);
            using var stream = new InMemoryRandomAccessStream();
            var renderOptions = CreateRenderOptions(page);
            await page.RenderToStreamAsync(stream, renderOptions);
            stream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(stream);
            var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            var result = await engine.RecognizeAsync(bitmap);
            var words = result.Lines
                .SelectMany(line => line.Words)
                .Select(word => new OcrWord(
                    word.Text,
                    word.BoundingRect.X,
                    word.BoundingRect.Y,
                    word.BoundingRect.Width,
                    word.BoundingRect.Height))
                .ToList();

            pages.Add(new OcrPage(
                PageNumber: (int)index + 1,
                Width: bitmap.PixelWidth,
                Height: bitmap.PixelHeight,
                Words: words));
        }

        return pages;
    }

    private static PdfPageRenderOptions CreateRenderOptions(PdfPage page)
    {
        return new PdfPageRenderOptions
        {
            DestinationWidth = Math.Max(1, (uint)Math.Round(page.Size.Width * RenderScale)),
            DestinationHeight = Math.Max(1, (uint)Math.Round(page.Size.Height * RenderScale))
        };
    }

    private static OcrEngine? CreateEngine()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var language = OcrEngine.AvailableRecognizerLanguages
            .FirstOrDefault(item => item.LanguageTag.Equals("zh-Hant-TW", StringComparison.OrdinalIgnoreCase))
            ?? OcrEngine.AvailableRecognizerLanguages
                .FirstOrDefault(item => item.LanguageTag.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase))
            ?? OcrEngine.AvailableRecognizerLanguages
                .FirstOrDefault(item => item.LanguageTag.StartsWith("zh", StringComparison.OrdinalIgnoreCase));

        return language is null
            ? OcrEngine.TryCreateFromUserProfileLanguages()
            : OcrEngine.TryCreateFromLanguage(language);
    }
}
