# Build 002 Import Engine Spec

## Goal

Build 002 turns Build 001 from a single-PDF preview tool into a real import engine. It must accept multiple files from Windows File Explorer, route each file to the correct brand/file parser, normalize song records, store them in SQLite, show import success rate, and export an updated `master.csv`.

## Scope

Build 002 includes:

- True Windows drag/drop for multiple files.
- Mixed drag/drop of PDF, Excel, and CSV files in one operation.
- InYuan PDF parser improvements.
- Golden Voice PDF parser foundation.
- InYuan Excel parser.
- Golden Voice Excel parser.
- CSV parser.
- Automatic column recognition for Excel and CSV.
- SQLite database foundation.
- Songs, Artists, Brands, Volumes, ImportHistory, Favorites tables.
- Deduplication and update comparison.
- Import summary with success/failure counts and success rate.

Build 002 excludes:

- Git commit, Git push, or GitHub PR creation.
- PDF visual preview.
- OCR fallback.
- Apple TV, Scriptable, YouTube, and network song lookup.

## User Workflow

1. User opens `manager-v6/src/KTVManagerProfessional.sln` in Visual Studio 2022 and presses F5.
2. User drags one or more files into the WPF window, or uses an import button.
3. App accepts files with extensions `.pdf`, `.xlsx`, `.xls`, and `.csv`.
4. App shows a queue with file name, detected brand, detected source type, detected volume, parser status, imported rows, failed rows, and success rate.
5. App writes successful records into SQLite.
6. App shows new, updated, duplicate, and failed records.
7. User can export `master.csv` from the SQLite song database.

## File Routing

The import engine must classify each file with this order:

1. File extension.
2. Filename brand hints, for example `音圓`, `金嗓`, `InYuan`, `GoldenVoice`.
3. Extracted content hints, for example PDF/Excel text containing brand name or known volume format.
4. Manual brand override only when automatic detection returns `Unknown`.

Supported routes:

| Extension | Brand | Parser |
| --- | --- | --- |
| `.pdf` | `音圓` | InYuan PDF parser |
| `.pdf` | `金嗓` | Golden Voice PDF parser |
| `.xlsx`, `.xls` | `音圓` | InYuan Excel parser |
| `.xlsx`, `.xls` | `金嗓` | Golden Voice Excel parser |
| `.csv` | any supported brand | CSV parser with column recognition |

Unsupported files must remain in the import queue with status `Unsupported`, not crash the app.

## Data Model

Canonical song fields:

- `SongNumber`: required, normalized as text.
- `Title`: required.
- `ArtistName`: optional for PDF rows when the source does not contain an artist.
- `Language`: optional, normalized to `台語`, `國語`, `華語`, `客語`, or `Unknown`.
- `BrandCode`: required, for example `音圓` or `金嗓`.
- `VolumeCode`: optional, normalized as text.
- `SourceFileName`: required.
- `SourceType`: `Pdf`, `Excel`, or `Csv`.
- `ImportedAt`: local timestamp.

## SQLite Schema

Database file:

`manager-v6/data/ktv-manager-v6.sqlite`

Tables:

### Brands

- `Id` integer primary key.
- `Code` text unique, required. Examples: `音圓`, `金嗓`.
- `DisplayName` text required.
- `CreatedAt` text required.

### Volumes

- `Id` integer primary key.
- `BrandId` integer required.
- `Code` text required.
- `DisplayName` text optional.
- `CreatedAt` text required.
- Unique key: `BrandId`, `Code`.

### Artists

- `Id` integer primary key.
- `Name` text unique, required.
- `NormalizedName` text required.
- `CreatedAt` text required.

### Songs

- `Id` integer primary key.
- `BrandId` integer required.
- `VolumeId` integer optional.
- `SongNumber` text required.
- `Title` text required.
- `NormalizedTitle` text required.
- `Language` text required.
- `PrimaryArtistId` integer optional.
- `SourceFileName` text required.
- `LastImportedAt` text required.
- Unique key: `BrandId`, `SongNumber`.

### SongArtists

- `SongId` integer required.
- `ArtistId` integer required.
- `SortOrder` integer required.
- Primary key: `SongId`, `ArtistId`.

### ImportHistory

- `Id` integer primary key.
- `SourceFileName` text required.
- `SourcePath` text required.
- `SourceType` text required.
- `DetectedBrandCode` text required.
- `DetectedVolumeCode` text optional.
- `StartedAt` text required.
- `FinishedAt` text optional.
- `TotalRows` integer required.
- `ImportedRows` integer required.
- `UpdatedRows` integer required.
- `DuplicateRows` integer required.
- `FailedRows` integer required.
- `SuccessRate` real required.
- `Status` text required.

### ImportIssues

- `Id` integer primary key.
- `ImportHistoryId` integer required.
- `LineNumber` integer optional.
- `CellReference` text optional.
- `RawText` text required.
- `Reason` text required.

### Favorites

- `Id` integer primary key.
- `SongId` integer required unique.
- `CreatedAt` text required.
- `Note` text optional.

## Deduplication

Primary deduplication key:

`BrandCode + SongNumber`

When a record with the same key already exists:

- If title, language, volume, or artist differs, classify as `Updated`.
- If every canonical field matches, classify as `Duplicate`.
- If the new record is missing optional data but the existing record has data, keep the existing data and classify as `Duplicate`.
- If the new record has artist or volume data that the existing record lacks, update the missing fields and classify as `Updated`.

## Automatic Column Recognition

Excel and CSV parsers must detect columns by header aliases and fallback scoring.

Song number aliases:

- `歌號`
- `歌曲編號`
- `編號`
- `SongNumber`
- `Song No`

Title aliases:

- `歌名`
- `歌曲名稱`
- `曲名`
- `Title`
- `Song`

Artist aliases:

- `歌手`
- `演唱者`
- `Artist`
- `Singer`

Language aliases:

- `語言`
- `Language`

Volume aliases:

- `集數`
- `期別`
- `Volume`

Brand aliases:

- `品牌`
- `廠牌`
- `Brand`

If headers are missing, the parser must inspect the first 20 non-empty rows and infer columns by value shape. For example, a mostly numeric 5- or 6-digit column is likely song number.

## PDF Parser Requirements

### InYuan PDF

Must support:

- Build 001 1326 format with type symbols.
- Build 001 1356 format without type symbols.
- Double-column text extraction where one physical line contains two or more songs, for example `201799 快醒雪 202528 美麗與哀愁`.
- Missing artist rows without treating the next song number as an artist.
- Language detection for `台語`, `國語`, `華語`, `客語`.
- Volume detection from filename or PDF text.

### Golden Voice PDF

Build 002 must create a separate parser class for Golden Voice. It can start with text-based parsing only, but it must not reuse InYuan parser rules directly. The Golden Voice parser must have its own tests and its own issue reporting.

## Import Success Rate

Success rate formula:

`ImportedRows + UpdatedRows + DuplicateRows` divided by `TotalRows`

Failed rows are rows with enough signal to be considered song-like but unable to produce a valid canonical record.

The UI must show:

- Total files.
- Completed files.
- Total rows.
- Imported rows.
- Updated rows.
- Duplicate rows.
- Failed rows.
- Success rate as a percentage with one decimal place.

## UI Requirements

Main window must contain:

- Import button.
- Drag/drop surface.
- Import queue grid.
- Song result grid.
- Import issues grid.
- Export `master.csv` button.
- Database path display.

The app must not use Windows Forms.

## Test Requirements

Tests must cover:

- Multi-file import request with PDF, Excel, and CSV inputs.
- Unsupported file is reported, not thrown.
- InYuan 1326 double-column PDF text.
- InYuan 1356 no-symbol PDF text.
- Golden Voice parser smoke test using representative text.
- Excel column recognition with Chinese headers.
- CSV column recognition with English headers.
- Deduplication: new, duplicate, and updated rows.
- SQLite insert and query roundtrip.
- Import success rate calculation.

## Acceptance Criteria

Build 002 is accepted when:

1. Visual Studio opens `manager-v6/src/KTVManagerProfessional.sln` without manual csproj edits.
2. `dotnet restore`, `dotnet build`, `dotnet test`, and Rebuild all pass with 0 errors.
3. User can drag multiple PDF/Excel/CSV files from Windows File Explorer into the WPF window.
4. App imports supported files into SQLite.
5. App reports unsupported files without crashing.
6. App shows success rate.
7. App exports UTF-8 `master.csv` from SQLite.
8. Unit tests cover parser, deduplication, database, and import summary behavior.
