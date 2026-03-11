## Summary

Adjusted symbolic-link creation failure handling so headless callers do not block on `MessageBox` when the machine lacks symlink privileges.

## Details

- `FileListViewModel.CreateSymbolicLinkWithHistory` now rethrows when there is no main window available, preserving the original exception for smoke tests and Ralph-loop runs.
- Added a smoke regression that injects a throwing filesystem service and verifies the privilege failure reaches the caller and does not create undo history.

## Lessons

- UI error paths that are acceptable in interactive WPF flows can still violate the Ralph-loop contract if they open modal dialogs during smoke coverage.
- Privilege-dependent features need explicit non-modal test hooks so failures can be asserted instead of skipped only by chance.
