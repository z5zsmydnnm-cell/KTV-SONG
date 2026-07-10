# Build 001 Design

## Goal

Create a Windows WPF project that Visual Studio 2022 Community can open, restore, build, rebuild, and run with F5. Build 001 imports InYuan PDFs, parses song rows, displays the result, and exports UTF-8 `master.csv`.

## Architecture

`KTVManagerProfessional.App` is the WPF UI for file picking, file drag/drop, grid display, parse issue display, and CSV export. `KTVManagerProfessional.Core` contains testable logic for PDF text extraction, InYuan text parsing, CSV export, and data models. `KTVManagerProfessional.Tests` uses xUnit to validate 1326 and 1356 parser formats, multiline merge behavior, failed line reporting, and UTF-8 CSV output.

## Parser Rules

- Brand code is `音圓`.
- Volume is detected from the source file name first, then from the PDF text.
- Languages supported: `台語`, `國語`, `華語`, `客語`.
- Ignored text includes `勃露斯`, `吉魯巴`, `Shuffle`, `R&B`, `音圓唱片`, and `歌曲類型說明`.
- Song rows may start with symbols such as `◆`, or may start directly with the song number.
- Two or more spaces separate title and artist.
- Song continuation lines without that separator merge into the title until the artist appears.
- Song-like lines missing title or artist are reported as parse issues.

## UI Rules

The app does not reference Windows Forms. File picking uses `Microsoft.Win32.OpenFileDialog`; drag/drop uses WPF `DataFormats.FileDrop`.

## Validation

```powershell
dotnet restore manager-v6/src/KTVManagerProfessional.sln
dotnet build manager-v6/src/KTVManagerProfessional.sln --configuration Release
dotnet test manager-v6/src/KTVManagerProfessional.sln --configuration Release --no-build
```
