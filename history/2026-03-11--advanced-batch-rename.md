Advanced batch rename shipped through the existing rename command path instead of adding a separate command surface. Multi-select `Rename`/`F2` now opens `BatchRenameDialog`, while single-item rename still uses the inline rename popup.

`BatchRenameService` owns both preview generation and execution. Preview generation applies search/replace, case transforms, counters, and extension rules, then resolves sibling-name collisions with the same numeric-suffix style used elsewhere in the app. Execution uses a two-phase rename: every selected item is staged to a temporary unique name first, then moved to its final target name. That avoids swap/collision failures and gives undo/redo a deterministic grouped operation.

Smoke coverage focuses on the acceptance-critical behavior:
- multi-select rename routes through the dialog path
- preview output reflects search/replace, case, counters, and collision handling
- one undo/redo entry restores and reapplies the whole batch
