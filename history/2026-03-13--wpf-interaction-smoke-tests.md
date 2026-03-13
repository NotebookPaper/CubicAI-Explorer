Spec 022 added the first dispatcher-level interaction tests to the smoke harness instead of expanding the existing viewmodel-only checks.

Key decisions:
- Created hidden `MainWindow` instances on the WPF dispatcher and pumped the dispatcher explicitly so the tests stay deterministic and headless-safe.
- Reached private interaction methods and template-owned controls through reflection instead of widening the production surface just for tests.
- Chose tab-strip overflow as the non-bookmark interaction case because it exercises real WPF layout, container generation, and routed menu clicks without requiring synthetic `DragEventArgs`.

Lessons and failure modes:
- `TreeViewItem.ActualHeight` includes visible child rows for expanded items, so drag-hit assertions must use points that stay within the row header band rather than the bottom of the expanded subtree.
- Hover-expand assertions must recompute their target point after collapsing the folder because the row bounds change immediately.
- The bookmark drag hit-testing regression is now directly guarded under mouse capture, which is where `InputHitTest` had previously been too fragile.
