# Build 003 GitHub Engine Spec

## Goal

Build 003 adds a real GitHub publishing workflow for the local repository at `D:\GitHub\KTV-SONG`. It must detect repository state, show changes, commit selected outputs, push to the current branch, and record release history. It must not provide a fake one-click publish button.

## Scope

Build 003 includes:

- Repository auto-detection.
- Git status display.
- Current branch display.
- Remote URL display.
- One-click commit for application-generated outputs.
- One-click push after a successful commit.
- Publish history stored in SQLite.
- Release notes generated from import history.

Build 003 excludes:

- GitHub Actions debugging.
- Pull request creation from inside the WPF app.
- Force push.
- Branch deletion.
- Credential management UI.
- GitHub Desktop automation.

## Repository Path

Default repository path:

`D:\GitHub\KTV-SONG`

The app must verify:

- Path exists.
- `.git` exists.
- `manager-v6/src/KTVManagerProfessional.sln` exists.
- Remote `origin` exists.

If any check fails, GitHub actions must be disabled and the UI must show a clear reason.

## Git Execution Model

The app may call the installed `git.exe` process directly. It must not shell out through `cmd.exe` with string-concatenated commands.

Required process safety:

- Use explicit executable path or `git` found through PATH.
- Use argument arrays, not command string concatenation.
- Set working directory to `D:\GitHub\KTV-SONG`.
- Capture stdout, stderr, and exit code.
- Apply a timeout.
- Never run destructive commands such as `reset --hard`, `clean -fd`, branch deletion, or force push.

## Git Status

The UI must show:

- Repository path.
- Current branch.
- Remote origin URL.
- Ahead/behind status when available.
- Changed files grouped by status:
  - Modified
  - Added
  - Deleted
  - Untracked

The app must make it obvious when there are unrelated user changes. One-click commit must only stage application-owned paths unless the user explicitly checks additional files.

Application-owned paths:

- `songs/master.csv`
- `songs/*.csv`
- `manager-v6/data/*.sqlite`
- `manager-v6/releases/**`
- `manager-v6/docs/release-notes/**`

Build and temporary output must remain ignored.

## One-Click Commit

The commit flow must be:

1. Refresh Git status.
2. Select only application-owned changed files by default.
3. Show the exact file list before commit.
4. Generate a commit message.
5. Run `git add` for the selected files.
6. Run `git commit`.
7. Save commit result to SQLite `PublishHistory`.
8. Refresh status.

Default commit message format:

`data: update KTV song database YYYY-MM-DD HH:mm`

The user may edit the message before committing.

If there are no selected changed files, commit button must be disabled.

## One-Click Push

The push flow must be:

1. Require a successful commit in the current session, or require the user to explicitly confirm pushing existing unpushed commits.
2. Show branch and remote target.
3. Run `git push`.
4. Capture stdout, stderr, and exit code.
5. Save result to SQLite `PublishHistory`.
6. Show success or failure details.

Push must never run automatically after import. It must always be a user action.

## PublishHistory Table

Build 003 extends SQLite with:

- `Id` integer primary key.
- `Action` text required: `Commit`, `Push`, or `ReleaseNote`.
- `RepositoryPath` text required.
- `BranchName` text optional.
- `RemoteUrl` text optional.
- `CommitSha` text optional.
- `Message` text optional.
- `SelectedFilesJson` text optional.
- `StartedAt` text required.
- `FinishedAt` text optional.
- `ExitCode` integer optional.
- `StdOut` text optional.
- `StdErr` text optional.
- `Status` text required.

## Release Notes

Build 003 must generate a markdown release note from the latest import history.

Release note path:

`manager-v6/docs/release-notes/YYYY-MM-DD-HHmm.md`

Release note content:

- Import date and time.
- Files imported.
- New songs.
- Updated songs.
- Duplicates skipped.
- Failed rows.
- Success rate.
- Exported CSV path.

Release notes are application-owned files and may be selected for commit.

## UI Requirements

Build 003 adds a GitHub tab or panel with:

- Repository status.
- Changed file list with checkboxes.
- Commit message box.
- Commit button.
- Push button.
- Command output viewer.
- Publish history list.

The UI must clearly distinguish:

- `Ready to commit`.
- `Nothing to commit`.
- `Unrelated changes present`.
- `Commit failed`.
- `Push failed`.
- `Authentication required`.

## Error Handling

Common errors must be shown with actionable text:

- Git not installed.
- Repository path missing.
- Not a Git repository.
- No remote origin.
- Authentication failed.
- Push rejected.
- Merge required.
- Nothing to commit.

The app must not hide stderr.

## Test Requirements

Tests must cover:

- Repository path validation.
- Parsing `git status --porcelain=v1` output.
- Selecting application-owned paths only.
- Commit message generation.
- Git command wrapper captures exit code/stdout/stderr.
- Push result parsing.
- PublishHistory insert/query roundtrip.
- Release note generation.

Git process tests should use a temporary test repository created during the test run. Tests must not commit to or push from `D:\GitHub\KTV-SONG`.

## Acceptance Criteria

Build 003 is accepted when:

1. GitHub panel reads repository path `D:\GitHub\KTV-SONG`.
2. Git status is visible in the WPF app.
3. User can commit selected application-owned changes.
4. User can push committed changes to origin.
5. App records commit and push attempts in SQLite.
6. App never stages unrelated user changes by default.
7. All git command failures show stderr and exit code.
8. `dotnet restore`, `dotnet build`, `dotnet test`, and Rebuild all pass with 0 errors.
