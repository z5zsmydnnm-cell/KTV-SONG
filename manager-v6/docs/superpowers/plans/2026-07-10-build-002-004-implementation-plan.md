# Build 002-004 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Build 002 Import Engine, Build 003 GitHub Engine, and Build 004 Preview/OCR in the existing WPF app while keeping Visual Studio 2022 F5, restore, build, rebuild, and tests passing.

**Architecture:** Keep `KTVManagerProfessional.App` as a thin WPF shell and put import, database, parser, Git, preview, diagnostics, and OCR decision logic in `KTVManagerProfessional.Core`. Build 002 creates the database-backed import pipeline; Build 003 adds safe Git command orchestration over application-owned files; Build 004 adds PDF page diagnostics, preview view-models, OCR availability/fallback, and manual correction flow.

**Tech Stack:** C# 12, .NET 8, WPF, xUnit, iText 9.7.0, Microsoft.Data.Sqlite 10.0.9, ClosedXML 0.105.0, process-based `git.exe`, Windows-compatible OCR abstraction with verified implementation or explicit unavailable state.

## Global Constraints

- Repository path is `D:\GitHub\KTV-SONG`.
- Solution path is `manager-v6/src/KTVManagerProfessional.sln`.
- No Windows Forms.
- No fake buttons. If an action cannot complete the real workflow, the button must be disabled or absent.
- No destructive Git commands.
- NuGet packages must be real, stable, and restorable from nuget.org at implementation time.
- Every parser behavior must have unit tests.
- Every database write path must have tests.
- Every build must keep Visual Studio 2022 F5 working.
- Build 003 tests must not commit to or push from `D:\GitHub\KTV-SONG`.

---

## File Structure

Create or modify these files:

- `manager-v6/src/KTVManagerProfessional.Core/Importing/ImportSourceType.cs`: enum for `Pdf`, `Excel`, `Csv`, `Unsupported`.
- `manager-v6/src/KTVManagerProfessional.Core/Importing/BrandCode.cs`: constants for `音圓`, `金嗓`, `Unknown`.
- `manager-v6/src/KTVManagerProfessional.Core/Importing/ImportFileRequest.cs`: one file import request.
- `manager-v6/src/KTVManagerProfessional.Core/Importing/ImportFileResult.cs`: per-file import result.
- `manager-v6/src/KTVManagerProfessional.Core/Importing/ImportSummary.cs`: aggregate counts and success rate.
- `manager-v6/src/KTVManagerProfessional.Core/Importing/ImportRouter.cs`: classifies files and selects parsers.
- `manager-v6/src/KTVManagerProfessional.Core/Importing/ImportEngine.cs`: orchestrates parse, dedupe, SQLite persistence, and summary.
- `manager-v6/src/KTVManagerProfessional.Core/Parsing/ISongParser.cs`: parser interface.
- `manager-v6/src/KTVManagerProfessional.Core/Parsing/InYuanPdfSongParser.cs`: wraps and evolves existing InYuan parser.
- `manager-v6/src/KTVManagerProfessional.Core/Parsing/GoldenVoicePdfSongParser.cs`: Golden Voice text parser foundation.
- `manager-v6/src/KTVManagerProfessional.Core/Parsing/TabularSongParser.cs`: Excel/CSV row parser after column mapping.
- `manager-v6/src/KTVManagerProfessional.Core/Parsing/ColumnRecognizer.cs`: header alias and fallback scoring.
- `manager-v6/src/KTVManagerProfessional.Core/Parsing/CsvSongParser.cs`: CSV parser.
- `manager-v6/src/KTVManagerProfessional.Core/Parsing/ExcelSongParser.cs`: Excel parser using ClosedXML.
- `manager-v6/src/KTVManagerProfessional.Core/Data/KtvDatabase.cs`: SQLite connection/migration helper.
- `manager-v6/src/KTVManagerProfessional.Core/Data/SongRepository.cs`: song, artist, brand, volume, history writes.
- `manager-v6/src/KTVManagerProfessional.Core/Data/ImportHistoryRepository.cs`: import issue/history writes.
- `manager-v6/src/KTVManagerProfessional.Core/Deduplication/SongDeduplicator.cs`: new/updated/duplicate classifier.
- `manager-v6/src/KTVManagerProfessional.Core/Git/GitCommandRunner.cs`: safe process wrapper.
- `manager-v6/src/KTVManagerProfessional.Core/Git/GitStatusParser.cs`: parses `git status --porcelain=v1`.
- `manager-v6/src/KTVManagerProfessional.Core/Git/GitRepositoryService.cs`: status, commit, push, release-note orchestration.
- `manager-v6/src/KTVManagerProfessional.Core/Preview/PdfDocumentInfo.cs`: PDF metadata.
- `manager-v6/src/KTVManagerProfessional.Core/Preview/PdfTextLayerExtractor.cs`: page text extraction.
- `manager-v6/src/KTVManagerProfessional.Core/Preview/PdfPreviewViewModel.cs`: preview navigation state.
- `manager-v6/src/KTVManagerProfessional.Core/Ocr/OcrDecision.cs`: OCR decision rules.
- `manager-v6/src/KTVManagerProfessional.Core/Ocr/OcrTextExtractor.cs`: OCR interface and Windows availability wrapper.
- `manager-v6/src/KTVManagerProfessional.Core/Corrections/ManualCorrectionService.cs`: accepts/ignores corrections.
- `manager-v6/src/KTVManagerProfessional.App/MainWindow.xaml`: replace single grid with tabs/panels for Import, GitHub, Preview/OCR.
- `manager-v6/src/KTVManagerProfessional.App/MainWindow.xaml.cs`: bind UI events to Core services.
- `manager-v6/tests/KTVManagerProfessional.Tests/*Tests.cs`: focused tests for each service.
- `manager-v6/README.md`: update usage instructions.

## Task 1: Add Stable Dependencies

**Files:**
- Modify: `manager-v6/src/KTVManagerProfessional.Core/KTVManagerProfessional.Core.csproj`
- Modify: `manager-v6/tests/KTVManagerProfessional.Tests/KTVManagerProfessional.Tests.csproj`

**Interfaces:**
- Produces: restored packages for SQLite and Excel parsing.

- [ ] **Step 1: Verify package versions before editing**

Run:

```powershell
dotnet add D:\GitHub\KTV-SONG\manager-v6\src\KTVManagerProfessional.Core\KTVManagerProfessional.Core.csproj package Microsoft.Data.Sqlite --version 10.0.9
dotnet add D:\GitHub\KTV-SONG\manager-v6\src\KTVManagerProfessional.Core\KTVManagerProfessional.Core.csproj package ClosedXML --version 0.105.0
```

Expected: both packages exist and are compatible with `net8.0`.

- [ ] **Step 2: Add packages**

Run:

```powershell
dotnet add D:\GitHub\KTV-SONG\manager-v6\src\KTVManagerProfessional.Core\KTVManagerProfessional.Core.csproj package Microsoft.Data.Sqlite --version 10.0.9
dotnet add D:\GitHub\KTV-SONG\manager-v6\src\KTVManagerProfessional.Core\KTVManagerProfessional.Core.csproj package ClosedXML --version 0.105.0
```

- [ ] **Step 3: Restore**

Run:

```powershell
dotnet restore D:\GitHub\KTV-SONG\manager-v6\src\KTVManagerProfessional.sln
```

Expected: restore succeeds with 0 errors.

## Task 2: Import Domain Models And Summary

**Files:**
- Create: `manager-v6/src/KTVManagerProfessional.Core/Importing/ImportSourceType.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Importing/BrandCode.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Importing/ImportFileRequest.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Importing/ImportFileResult.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Importing/ImportSummary.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/ImportSummaryTests.cs`

**Interfaces:**
- Produces: `ImportSummary.FromResults(IEnumerable<ImportFileResult>)`.

- [ ] **Step 1: Write failing success-rate test**

Create `ImportSummaryTests.cs` with:

```csharp
using KTVManagerProfessional.Core.Importing;

namespace KTVManagerProfessional.Tests;

public sealed class ImportSummaryTests
{
    [Fact]
    public void FromResults_calculates_success_rate_from_imported_updated_and_duplicates()
    {
        var result = new ImportFileResult(
            SourcePath: @"D:\in\1326.pdf",
            SourceFileName: "1326.pdf",
            SourceType: ImportSourceType.Pdf,
            BrandCode: "音圓",
            VolumeCode: "1326",
            TotalRows: 10,
            ImportedRows: 4,
            UpdatedRows: 2,
            DuplicateRows: 1,
            FailedRows: 3,
            Issues: []);

        var summary = ImportSummary.FromResults([result]);

        Assert.Equal(1, summary.TotalFiles);
        Assert.Equal(10, summary.TotalRows);
        Assert.Equal(7, summary.SuccessfulRows);
        Assert.Equal(3, summary.FailedRows);
        Assert.Equal(70.0, summary.SuccessRate);
    }
}
```

- [ ] **Step 2: Run red test**

Run:

```powershell
dotnet test D:\GitHub\KTV-SONG\manager-v6\src\KTVManagerProfessional.sln --no-restore --filter FullyQualifiedName~ImportSummaryTests
```

Expected: compile fails because `ImportSummary` does not exist.

- [ ] **Step 3: Implement models**

Create the files listed above with immutable records and `ImportSummary.FromResults`.

- [ ] **Step 4: Run green test**

Run the same filtered test.

Expected: 1 passed, 0 failed.

## Task 3: File Routing

**Files:**
- Create: `manager-v6/src/KTVManagerProfessional.Core/Importing/ImportRoute.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Importing/ImportRouter.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/ImportRouterTests.cs`

**Interfaces:**
- Consumes: `ImportSourceType`, `BrandCode`.
- Produces: `ImportRouter.Route(string path, string? contentHint = null): ImportRoute`.

- [ ] **Step 1: Write failing route tests**

Tests:

```csharp
[Fact]
public void Route_detects_inyuan_pdf_from_filename()
{
    var route = ImportRouter.Route(@"D:\in\音圓1326.pdf");
    Assert.Equal(ImportSourceType.Pdf, route.SourceType);
    Assert.Equal("音圓", route.BrandCode);
    Assert.False(route.IsUnsupported);
}

[Fact]
public void Route_reports_unsupported_extension()
{
    var route = ImportRouter.Route(@"D:\in\readme.txt");
    Assert.True(route.IsUnsupported);
    Assert.Equal(ImportSourceType.Unsupported, route.SourceType);
}
```

- [ ] **Step 2: Run red tests**

Expected: compile failure.

- [ ] **Step 3: Implement router**

Implement extension detection for `.pdf`, `.xlsx`, `.xls`, `.csv` and brand hints for `音圓`, `金嗓`, `InYuan`, `GoldenVoice`.

- [ ] **Step 4: Run green tests**

Expected: route tests pass.

## Task 4: Parser Interfaces And Golden Voice Foundation

**Files:**
- Create: `manager-v6/src/KTVManagerProfessional.Core/Parsing/ISongParser.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Parsing/InYuanPdfSongParser.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Parsing/GoldenVoicePdfSongParser.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/GoldenVoicePdfSongParserTests.cs`

**Interfaces:**
- Produces: `ISongParser.Parse(string text, string sourceName): ParseResult`.

- [ ] **Step 1: Write failing Golden Voice smoke test**

Test a representative text block:

```csharp
var text = """
金嗓 112
國語
12345 月亮代表我的心  鄧麗君
12346 心事誰人知  沈文程
""";
var result = new GoldenVoicePdfSongParser().Parse(text, "金嗓112.pdf");
Assert.Equal(2, result.Songs.Count);
Assert.Contains(result.Songs, s => s.BrandCode == "金嗓" && s.SongNumber == "12345");
```

- [ ] **Step 2: Run red test**

Expected: compile failure.

- [ ] **Step 3: Implement parser interface and wrappers**

`InYuanPdfSongParser` delegates to existing `InYuanSongParser.ParseText`. `GoldenVoicePdfSongParser` uses its own regex and does not call InYuan parser.

- [ ] **Step 4: Run parser tests**

Expected: InYuan and Golden Voice tests pass.

## Task 5: Column Recognition, CSV, And Excel

**Files:**
- Create: `manager-v6/src/KTVManagerProfessional.Core/Parsing/ColumnRecognizer.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Parsing/TabularSongParser.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Parsing/CsvSongParser.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Parsing/ExcelSongParser.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/ColumnRecognizerTests.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/CsvSongParserTests.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/ExcelSongParserTests.cs`

**Interfaces:**
- Produces: `ColumnRecognizer.Recognize(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> sampleRows)`.
- Produces: `CsvSongParser.ParseFile(string path, string brandCode)`.
- Produces: `ExcelSongParser.ParseFile(string path, string brandCode)`.

- [ ] **Step 1: Write failing header-recognition tests**

Chinese header test maps `歌號`, `歌名`, `歌手`, `語言`, `集數`. English header test maps `SongNumber`, `Title`, `Artist`, `Language`, `Volume`.

- [ ] **Step 2: Run red tests**

Expected: compile failure.

- [ ] **Step 3: Implement recognizer and CSV parser**

Use `TextFieldParser`-style CSV handling through a small quoted-field parser or `CsvHelper` only if verified as a stable NuGet. Prefer local parser for Build 002.

- [ ] **Step 4: Implement Excel parser**

Use ClosedXML to read the first worksheet, first non-empty row as header, then parse rows.

- [ ] **Step 5: Run tests**

Expected: column, CSV, and Excel tests pass.

## Task 6: SQLite Database And Repositories

**Files:**
- Create: `manager-v6/src/KTVManagerProfessional.Core/Data/KtvDatabase.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Data/SongRepository.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Data/ImportHistoryRepository.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/KtvDatabaseTests.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/SongRepositoryTests.cs`

**Interfaces:**
- Produces: `KtvDatabase.Initialize(string databasePath)`.
- Produces: `SongRepository.UpsertSong(SongRecord song, string sourceFileName, DateTimeOffset importedAt): SongWriteResult`.

- [ ] **Step 1: Write failing migration test**

Test creates a temp `.sqlite`, calls `KtvDatabase.Initialize`, and asserts tables `Brands`, `Volumes`, `Artists`, `Songs`, `SongArtists`, `ImportHistory`, `ImportIssues`, `Favorites` exist.

- [ ] **Step 2: Run red test**

Expected: compile failure.

- [ ] **Step 3: Implement migrations**

Use `Microsoft.Data.Sqlite` and `CREATE TABLE IF NOT EXISTS`.

- [ ] **Step 4: Write failing song upsert tests**

Test new, duplicate, and updated rows using `BrandCode + SongNumber`.

- [ ] **Step 5: Implement repositories**

Insert brands/artists/volumes as needed. Preserve existing richer data when new row is missing optional fields.

- [ ] **Step 6: Run database tests**

Expected: database tests pass.

## Task 7: Import Engine

**Files:**
- Create: `manager-v6/src/KTVManagerProfessional.Core/Importing/ImportEngine.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/ImportEngineTests.cs`

**Interfaces:**
- Consumes: router, parsers, repositories.
- Produces: `ImportEngine.ImportFilesAsync(IReadOnlyList<string> paths, string databasePath, CancellationToken cancellationToken): Task<ImportSummary>`.

- [ ] **Step 1: Write failing mixed-file import test**

Use temp CSV and unsupported `.txt`; assert CSV imports and unsupported file appears in results without throwing.

- [ ] **Step 2: Run red test**

Expected: compile failure.

- [ ] **Step 3: Implement import engine**

Route each file, parse, persist, record history/issues, aggregate summary.

- [ ] **Step 4: Run import engine tests**

Expected: mixed-file import tests pass.

## Task 8: Build 002 WPF Import UI

**Files:**
- Modify: `manager-v6/src/KTVManagerProfessional.App/MainWindow.xaml`
- Modify: `manager-v6/src/KTVManagerProfessional.App/MainWindow.xaml.cs`
- Create: `manager-v6/src/KTVManagerProfessional.App/ViewModels/ImportQueueItemViewModel.cs`

**Interfaces:**
- Consumes: `ImportEngine`.
- Produces: multi-file drag/drop and import summary display.

- [ ] **Step 1: Build UI skeleton**

Add tabs: `Import`, `GitHub`, `Preview/OCR`. In Build 002, GitHub and Preview/OCR tabs may show disabled states until their tasks are implemented.

- [ ] **Step 2: Wire multi-file drag/drop**

Accept `.pdf`, `.xlsx`, `.xls`, `.csv` and show unsupported files in queue.

- [ ] **Step 3: Wire import button**

Use WPF `OpenFileDialog` with `Multiselect = true`.

- [ ] **Step 4: Run WPF build**

Run:

```powershell
dotnet build D:\GitHub\KTV-SONG\manager-v6\src\KTVManagerProfessional.sln --configuration Debug --no-restore
```

Expected: 0 errors.

## Task 9: Git Command Core

**Files:**
- Create: `manager-v6/src/KTVManagerProfessional.Core/Git/GitCommandResult.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Git/GitCommandRunner.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Git/GitStatusEntry.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Git/GitStatusParser.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/GitStatusParserTests.cs`

**Interfaces:**
- Produces: `GitStatusParser.ParsePorcelain(string output): IReadOnlyList<GitStatusEntry>`.

- [ ] **Step 1: Write failing status parser tests**

Parse modified, added, deleted, and untracked examples from `git status --porcelain=v1`.

- [ ] **Step 2: Run red test**

Expected: compile failure.

- [ ] **Step 3: Implement parser and command runner**

Runner uses `ProcessStartInfo.ArgumentList`, working directory, timeout, stdout/stderr capture.

- [ ] **Step 4: Run tests**

Expected: Git parser tests pass.

## Task 10: Git Repository Service And Publish History

**Files:**
- Create: `manager-v6/src/KTVManagerProfessional.Core/Git/GitRepositoryService.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Git/ApplicationOwnedPathFilter.cs`
- Modify: `manager-v6/src/KTVManagerProfessional.Core/Data/KtvDatabase.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Data/PublishHistoryRepository.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/GitRepositoryServiceTests.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/PublishHistoryRepositoryTests.cs`

**Interfaces:**
- Produces: `GitRepositoryService.GetStatusAsync()`.
- Produces: `GitRepositoryService.CommitAsync(IEnumerable<string> selectedFiles, string message)`.
- Produces: `GitRepositoryService.PushAsync()`.

- [ ] **Step 1: Write failing application-owned path tests**

Assert `songs/master.csv`, `manager-v6/data/app.sqlite`, and `manager-v6/docs/release-notes/x.md` are selected; arbitrary source files are not selected by default.

- [ ] **Step 2: Implement filter**

- [ ] **Step 3: Write temporary-repo git tests**

Create a temp repo, commit an initial file, modify an application-owned file, and assert status/commit works without touching `D:\GitHub\KTV-SONG`.

- [ ] **Step 4: Implement service and publish history**

No `reset`, `clean`, branch delete, or force push commands.

- [ ] **Step 5: Run Git tests**

Expected: Git tests pass.

## Task 11: Build 003 WPF GitHub UI

**Files:**
- Modify: `manager-v6/src/KTVManagerProfessional.App/MainWindow.xaml`
- Modify: `manager-v6/src/KTVManagerProfessional.App/MainWindow.xaml.cs`
- Create: `manager-v6/src/KTVManagerProfessional.App/ViewModels/GitStatusViewModel.cs`

**Interfaces:**
- Consumes: `GitRepositoryService`.
- Produces: real status, commit, push UI.

- [ ] **Step 1: Add GitHub tab**

Show repository path, branch, remote, changed files, selected files, commit message, command output.

- [ ] **Step 2: Disable fake actions**

Commit button disabled when no selected application-owned file exists. Push button disabled when no push target exists.

- [ ] **Step 3: Wire commit and push**

Call service methods and show stdout/stderr.

- [ ] **Step 4: Run Debug build**

Expected: 0 errors.

## Task 12: Release Notes

**Files:**
- Create: `manager-v6/src/KTVManagerProfessional.Core/Git/ReleaseNoteGenerator.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/ReleaseNoteGeneratorTests.cs`

**Interfaces:**
- Produces: markdown file content and path `manager-v6/docs/release-notes/YYYY-MM-DD-HHmm.md`.

- [ ] **Step 1: Write failing release note test**

Given import summary, assert markdown contains imported, updated, duplicate, failed, and success rate fields.

- [ ] **Step 2: Implement generator**

- [ ] **Step 3: Run tests**

Expected: release note tests pass.

## Task 13: PDF Preview Diagnostics

**Files:**
- Create: `manager-v6/src/KTVManagerProfessional.Core/Preview/PdfDocumentInfo.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Preview/PdfTextLayerExtractor.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Preview/PdfImportDiagnostics.cs`
- Modify: `manager-v6/src/KTVManagerProfessional.Core/Data/KtvDatabase.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/PdfImportDiagnosticsTests.cs`

**Interfaces:**
- Produces: `PdfImportDiagnostics.Create(pageNumber, text, parseIssueCount)`.

- [ ] **Step 1: Write failing diagnostics tests**

Assert empty text has low confidence, pages with parse issues are flagged, and diagnostics rows can be inserted into SQLite.

- [ ] **Step 2: Implement diagnostics and database extensions**

- [ ] **Step 3: Run diagnostics tests**

Expected: tests pass.

## Task 14: OCR Decision And Availability

**Files:**
- Create: `manager-v6/src/KTVManagerProfessional.Core/Ocr/OcrDecision.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Ocr/IOcrTextExtractor.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Ocr/OcrAvailability.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/OcrDecisionTests.cs`

**Interfaces:**
- Produces: `OcrDecision.ShouldRun(string extractedText, int songLikeRowCount, double parserSuccessRate, bool userRequested)`.

- [ ] **Step 1: Write failing OCR decision tests**

Assert OCR runs for empty text, success rate below 60, user request; does not run when success rate is enough.

- [ ] **Step 2: Implement decision rules**

- [ ] **Step 3: Verify OCR package/API**

Before adding OCR dependency, verify Windows 11 compatibility and stable package/API. If no safe OCR implementation is available in this environment, implement an unavailable extractor that reports `OcrUnavailable` clearly and keeps UI non-crashing.

- [ ] **Step 4: Run tests**

Expected: OCR decision tests pass.

## Task 15: Manual Corrections

**Files:**
- Create: `manager-v6/src/KTVManagerProfessional.Core/Corrections/ManualCorrection.cs`
- Create: `manager-v6/src/KTVManagerProfessional.Core/Corrections/ManualCorrectionService.cs`
- Modify: `manager-v6/src/KTVManagerProfessional.Core/Data/KtvDatabase.cs`
- Test: `manager-v6/tests/KTVManagerProfessional.Tests/ManualCorrectionServiceTests.cs`

**Interfaces:**
- Produces: `ManualCorrectionService.AcceptCorrection(...)` and `IgnoreCorrection(...)`.

- [ ] **Step 1: Write failing correction tests**

Accepted correction inserts through `SongRepository`; ignored correction remains ignored and does not write a song.

- [ ] **Step 2: Implement service and table**

- [ ] **Step 3: Run tests**

Expected: correction tests pass.

## Task 16: Build 004 Preview/OCR UI

**Files:**
- Modify: `manager-v6/src/KTVManagerProfessional.App/MainWindow.xaml`
- Modify: `manager-v6/src/KTVManagerProfessional.App/MainWindow.xaml.cs`
- Create: `manager-v6/src/KTVManagerProfessional.App/ViewModels/PdfPreviewViewModel.cs`

**Interfaces:**
- Consumes: preview diagnostics, OCR decision, manual corrections.
- Produces: Preview/OCR tab.

- [ ] **Step 1: Add preview tab controls**

Include page navigation, extracted text, OCR text, issue list, correction editor, accept/ignore buttons.

- [ ] **Step 2: Wire non-blocking OCR state**

OCR unavailable must show clear status and must not crash.

- [ ] **Step 3: Run Debug build**

Expected: 0 errors.

## Task 17: Documentation Update

**Files:**
- Modify: `manager-v6/README.md`
- Modify: `manager-v6/docs/build-002-004-roadmap.md`

**Interfaces:**
- Produces: user instructions for import, GitHub panel, preview/OCR, database path, and known OCR availability notes.

- [ ] **Step 1: Update README**

Document Visual Studio open path, database path, supported import files, GitHub path, safe publish rules, and OCR availability.

- [ ] **Step 2: Update roadmap status**

Mark Build 002/003/004 implemented after verification.

## Task 18: Final Verification

**Files:**
- No code changes.

**Interfaces:**
- Verifies all acceptance criteria.

- [ ] **Step 1: Restore**

Run:

```powershell
dotnet restore D:\GitHub\KTV-SONG\manager-v6\src\KTVManagerProfessional.sln
```

Expected: 0 errors.

- [ ] **Step 2: Debug build**

Run:

```powershell
dotnet build D:\GitHub\KTV-SONG\manager-v6\src\KTVManagerProfessional.sln --configuration Debug --no-restore
```

Expected: 0 warnings, 0 errors.

- [ ] **Step 3: Release rebuild**

Run:

```powershell
dotnet build D:\GitHub\KTV-SONG\manager-v6\src\KTVManagerProfessional.sln --configuration Release --no-restore -t:Rebuild
```

Expected: 0 warnings, 0 errors.

- [ ] **Step 4: Tests**

Run:

```powershell
dotnet test D:\GitHub\KTV-SONG\manager-v6\src\KTVManagerProfessional.sln --configuration Release --no-build
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

Stage only intended files, excluding old untracked folders:

```powershell
git -c safe.directory=D:/GitHub/KTV-SONG -C D:\GitHub\KTV-SONG status -sb
git -c safe.directory=D:/GitHub/KTV-SONG -C D:\GitHub\KTV-SONG add manager-v6 .github .gitignore
git -c safe.directory=D:/GitHub/KTV-SONG -C D:\GitHub\KTV-SONG commit -m "feat: implement manager v6 builds 002 003 004"
git -c safe.directory=D:/GitHub/KTV-SONG -C D:\GitHub\KTV-SONG push
```

Expected: commit and push succeed on `agent/build-001-manager-v6`.

## Self-Review Notes

- Build 002 scope maps to Tasks 1-8.
- Build 003 scope maps to Tasks 9-12.
- Build 004 scope maps to Tasks 13-16.
- Documentation and final verification map to Tasks 17-18.
- OCR implementation is intentionally gated on real Windows-compatible availability verification; if unavailable, the accepted behavior is a clear unavailable state, not a fake OCR success.
