# KTV Manager Professional Build 001

Windows 11 / Visual Studio 2022 Community / C# / .NET 8 / WPF project.

## Open And Run

1. Open `manager-v6/src/KTVManagerProfessional.sln` in Visual Studio 2022 Community.
2. Set `KTVManagerProfessional.App` as the startup project.
3. Press F5.

Command-line verification:

```powershell
dotnet restore manager-v6/src/KTVManagerProfessional.sln
dotnet build manager-v6/src/KTVManagerProfessional.sln --configuration Release
dotnet test manager-v6/src/KTVManagerProfessional.sln --configuration Release --no-build
```

## Current Features

- WPF only; no Windows Forms types.
- Drag PDF, Excel, and CSV files from Windows File Explorer into the WPF window.
- Choose multiple PDF, Excel, and CSV files with a button.
- Extract PDF text with NuGet package `itext` 9.7.0.
- Parse InYuan PDF text:
  - Volume 1326 format with a leading `◆` type symbol.
  - Volume 1356 format without a leading type symbol.
- Parse Golden Voice text-based PDF rows with a dedicated parser.
- Parse CSV and Excel files with automatic column recognition.
- Store imported songs in SQLite at `manager-v6/data/ktv-manager-v6.sqlite`.
- Deduplicate by brand and song number.
- Show import queue counts and success rate.
- Display song number, title, artist, language, InYuan code, and volume.
- Export UTF-8 `master.csv`, one song per row.
- Show failed parse lines with line number, original text, and reason.
- Show Git repository status for `D:\GitHub\KTV-SONG`.
- Commit and push application-owned generated files only.
- Record publish history in SQLite.
- Provide PDF/OCR diagnostics storage and an OCR-unavailable state when no local OCR engine is configured.

## Still Not Included

- Apple TV
- Scriptable
- YouTube
- Cloud OCR

## Structure

```text
manager-v6/
  src/
    KTVManagerProfessional.sln
    KTVManagerProfessional.App/
    KTVManagerProfessional.Core/
  tests/
    KTVManagerProfessional.Tests/
  docs/
  samples/
  releases/
```

## License Note

PDF text extraction uses the `itext` NuGet package. Confirm iText licensing before commercial or closed-source distribution.
