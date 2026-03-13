Implemented `Undo Close Tab` as a session-local restore flow in `MainViewModel` rather than persisting recently closed tabs into settings. That keeps named-session save/load and startup restoration semantics stable and avoids reviving stale tabs across launches.

The closed-tab stack stores a cloned `TabItem` plus the original tab index. Restore inserts the tab back near its former position, reactivates it, and preserves lock/color metadata through the existing persisted-tab model. The stack is bounded to avoid unbounded session growth.

Tracking is explicitly suppressed during named-session application and viewmodel disposal so shutdown/session teardown does not pollute the undo-close history. That suppression boundary is important: without it, loading a named session would make unrelated tabs appear undoable.

Smoke coverage was added for:
- single close and immediate restore
- metadata restoration for locked/colorized tabs
- reverse-order restoration after multi-close flows, including close-right and close-other sequences

One validation wrinkle: WPF output locking can still cause transient `dotnet build` collisions if the smoke project is built while the smoke executable is already running. Sequential build then execute remains the reliable pattern.
