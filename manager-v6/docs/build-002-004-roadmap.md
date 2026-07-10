# Build 002-004 Roadmap

## Recommended Order

1. Build 002: Import Engine
2. Build 003: GitHub Engine
3. Build 004: Preview And OCR

This order keeps the product usable after every build. Import and database behavior must be stable before GitHub publishing is added. OCR and PDF preview depend on the import diagnostics created in Build 002.

## Build Boundary Summary

### Build 002

Make import real:

- Multi-file drag/drop.
- PDF, Excel, CSV import.
- InYuan and Golden Voice parser separation.
- SQLite storage.
- Deduplication and update comparison.
- Success rate display.

### Build 003

Make publish real:

- Repository auto-detection at `D:\GitHub\KTV-SONG`.
- Git status.
- Safe one-click commit.
- Safe one-click push.
- Publish history.

### Build 004

Make failures reviewable:

- PDF preview.
- Page diagnostics.
- OCR fallback.
- Manual correction queue.

## Cross-Build Rules

- No Windows Forms.
- No fake buttons. If an action cannot complete the real workflow, the button must be disabled or absent.
- No destructive Git commands.
- NuGet packages must be real, stable, and restorable from nuget.org at implementation time.
- Every parser behavior must have unit tests.
- Every database write path must have tests.
- Every build must keep Visual Studio 2022 F5 working.

## Implementation Gate

Do not start Build 002 implementation until these documents are reviewed:

- `build-002-import-engine-spec.md`
- `build-003-github-engine-spec.md`
- `build-004-preview-ocr-spec.md`

After review, create a task-by-task implementation plan for Build 002 only.
