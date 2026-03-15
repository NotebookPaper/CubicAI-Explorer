Spec 023 closed a second code-review pass focused on correctness and hardening rather than new features.

- Reworked folder-tree syncing to use a versioned async task flow so rapid pane navigation cannot leave stale tree expansions selecting the wrong node.
- Made saved-search replay await pane navigation completion, then kept smoke coverage focused on proving the async directory load no longer overwrites the replayed search result set.
- Tightened ZIP extraction and single-instance IPC bounds with separator-aware path checks and a capped raw-byte pipe reader.
- Replaced the flagged async-over-sync wrappers in settings, bookmarks, folder-tree loading, and file-list loading with true synchronous code paths to remove UI-thread deadlock risk without pushing async through the entire startup surface.
- Fixed command-line quoting for external tools at the placeholder expansion point because that is where selected-path injection actually occurs in the current app flow.
