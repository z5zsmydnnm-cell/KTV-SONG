# Build 004 Preview And OCR Spec

## Goal

Build 004 adds PDF preview and OCR fallback so users can inspect failed PDF imports, see where parsing failed, and recover text from scanned or image-heavy PDFs. OCR is a fallback, not the default path.

## Scope

Build 004 includes:

- PDF preview inside the WPF app.
- Page navigation.
- Highlighting pages with parse issues.
- Text extraction diagnostics.
- OCR fallback for pages with low text extraction confidence.
- OCR issue review workflow.
- Manual correction queue for failed rows.

Build 004 excludes:

- Cloud OCR services.
- YouTube lookup.
- Apple TV.
- Automatic online metadata enrichment.
- GitHub publish features beyond what Build 003 already provides.

## PDF Engine

Build 004 formalizes the PDF engine into separate components:

- `PdfDocumentLoader`: opens PDFs, reads page count, and provides page handles.
- `PdfTextLayerExtractor`: extracts text using the existing PDF text engine.
- `PdfPreviewRenderer`: renders pages to WPF-compatible images.
- `OcrTextExtractor`: runs OCR only when needed.
- `PdfImportDiagnostics`: records extraction confidence, parser confidence, and failure reasons.

The PDF engine must be brand-neutral. Brand-specific parsing stays in InYuan and Golden Voice parsers.

## PDF Preview

The preview panel must show:

- Current PDF filename.
- Page number and total page count.
- Previous/next page buttons.
- Zoom control.
- Rendered page image.
- Extracted text for the current page.
- Parse issue list for the current page.

If a PDF has failed rows, the preview must open to the first page that contains a parse issue.

## OCR Fallback

OCR fallback must run only when at least one of these is true:

- Extracted text is empty.
- Extracted text has fewer than a configured number of song-like rows.
- Parser success rate for the file is below the configured threshold.
- User manually clicks `Run OCR for this page`.

Default threshold:

- OCR candidate when parser success rate is below 60%.

OCR results must be marked as OCR-derived so they can be reviewed separately.

## OCR Engine Selection

Implementation must use a Windows-compatible OCR package or Windows OCR API that can be restored or used on Windows 11. The exact package/API must be verified during implementation before adding it.

The implementation must document:

- Package/API name.
- Version if using NuGet.
- Whether it requires local language packs.
- Supported recognition languages.
- Any licensing restrictions.

Build 004 must not add an OCR dependency that cannot run on the target Windows 11 environment.

## OCR Languages

OCR should prefer Traditional Chinese and English support.

Priority:

1. Traditional Chinese.
2. English.
3. Japanese only if a selected OCR engine requires it for mixed CJK recognition.

If Traditional Chinese OCR is unavailable, the app must show OCR as unavailable rather than silently returning poor results.

## Manual Correction Queue

Failed or low-confidence OCR rows must appear in a correction queue.

Each correction item includes:

- Source file.
- Page number.
- Raw extracted text.
- OCR text if available.
- Suggested song number.
- Suggested title.
- Suggested artist.
- Suggested language.
- Suggested brand.
- Suggested volume.
- Status: `NeedsReview`, `Accepted`, `Ignored`.

Accepted corrections are inserted through the same Build 002 import pipeline so deduplication and update comparison still apply.

## Confidence And Success Display

Build 004 must display two separate percentages:

- Text extraction success rate.
- Parser success rate.

OCR must not hide the original parser result. The user should be able to compare:

- PDF text layer result.
- OCR result.
- Final accepted import result.

## Data Model Extensions

Build 004 extends SQLite with:

### PdfPageDiagnostics

- `Id` integer primary key.
- `ImportHistoryId` integer required.
- `PageNumber` integer required.
- `TextLayerCharacterCount` integer required.
- `SongLikeRowCount` integer required.
- `ParserIssueCount` integer required.
- `OcrRan` integer required.
- `OcrCharacterCount` integer required.
- `Confidence` real required.
- `CreatedAt` text required.

### ManualCorrections

- `Id` integer primary key.
- `ImportHistoryId` integer required.
- `PageNumber` integer required.
- `RawText` text required.
- `OcrText` text optional.
- `SongNumber` text optional.
- `Title` text optional.
- `ArtistName` text optional.
- `Language` text optional.
- `BrandCode` text optional.
- `VolumeCode` text optional.
- `Status` text required.
- `CreatedAt` text required.
- `UpdatedAt` text optional.

## UI Requirements

Build 004 adds a Preview/OCR tab or split panel:

- PDF page preview.
- Page navigation controls.
- Extracted text viewer.
- OCR text viewer.
- Parse issue list.
- Manual correction editor.
- Accept correction button.
- Ignore correction button.

The UI must remain responsive. Long OCR work must run asynchronously with progress reporting and cancellation.

## Error Handling

The app must handle:

- Password-protected PDF.
- Damaged PDF.
- Image-only PDF.
- OCR engine unavailable.
- OCR language unavailable.
- OCR timeout.
- Page render failure.

Each error must be recorded in import diagnostics and shown in the UI.

## Test Requirements

Tests must cover:

- OCR decision rule when text extraction is empty.
- OCR decision rule when parser success rate is below 60%.
- OCR not running when parser success rate is sufficient.
- Page diagnostics creation.
- Manual correction accepted into canonical import pipeline.
- Manual correction ignored.
- Preview state opens first failed page.

UI rendering tests may be limited to view-model tests unless a reliable WPF UI automation strategy is added.

## Acceptance Criteria

Build 004 is accepted when:

1. User can preview PDF pages inside the WPF app.
2. User can navigate pages.
3. App identifies pages with parse issues.
4. OCR can be run for low-confidence pages.
5. OCR unavailable states are clear and non-crashing.
6. User can accept manual corrections.
7. Accepted corrections flow through SQLite deduplication.
8. `dotnet restore`, `dotnet build`, `dotnet test`, and Rebuild all pass with 0 errors.
