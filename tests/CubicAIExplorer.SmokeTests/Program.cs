using System.IO;
using CubicAIExplorer.Models;
using CubicAIExplorer.Services;
using CubicAIExplorer.ViewModels;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var failures = new List<string>();
        var tempRoot = Path.Combine(Path.GetTempPath(), "CubicAIExplorer_Smoke_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            Run("rename event + rename commit", failures, () => TestRenameFlow(tempRoot));
            Run("undo rename", failures, () => TestUndoRename(tempRoot));
            Run("redo rename", failures, () => TestRedoRename(tempRoot));
            Run("multi-select copy command", failures, () => TestMultiSelectCopy(tempRoot));
            Run("drop import copy + move", failures, () => TestDropImport(tempRoot));
            Run("undo copy", failures, () => TestUndoCopy(tempRoot));
            Run("undo move", failures, () => TestUndoMove(tempRoot));
            Run("undo permanent delete", failures, () => TestUndoPermanentDelete(tempRoot));
            Run("clear history", failures, () => TestClearHistory(tempRoot));
            Run("select-all event", failures, () => TestSelectAllEvent(tempRoot));
            Run("create folder collision suffix", failures, () => TestCreateFolderCollision(tempRoot));
            Run("move same folder no-op", failures, () => TestMoveSameFolderNoOp(tempRoot));
            Run("shell icon service", failures, () => TestShellIconService(tempRoot));
            Run("bookmarks add + dedupe", failures, () => TestBookmarks(tempRoot));
            Run("redo copy", failures, () => TestRedoCopy(tempRoot));
            Run("redo move", failures, () => TestRedoMove(tempRoot));
            Run("view mode property", failures, () => TestViewModeProperty(tempRoot));
            Run("selection status text", failures, () => TestSelectionStatus(tempRoot));
            Run("filter text", failures, () => TestFilterText(tempRoot));
            Run("properties command", failures, () => TestPropertiesCommand(tempRoot));
            Run("duplicate tab", failures, () => TestDuplicateTab(tempRoot));
            Run("close other tabs", failures, () => TestCloseOtherTabs(tempRoot));
            Run("selection size status", failures, () => TestSelectionSizeStatus(tempRoot));
            Run("breadcrumb segments", failures, () => TestBreadcrumbSegments(tempRoot));
            Run("recent folders", failures, () => TestRecentFolders(tempRoot));
            Run("search in folder", failures, () => TestSearchInFolder(tempRoot));
            Run("search close and clear", failures, () => TestSearchCloseAndClear(tempRoot));
            Run("dual pane toggle", failures, () => TestDualPaneToggle(tempRoot));
            Run("active pane command routing", failures, () => TestActivePaneCommandRouting(tempRoot));
            Run("active pane ui command routing", failures, () => TestActivePaneUiCommandRouting(tempRoot));
            Run("active pane view search routing", failures, () => TestActivePaneViewSearchRouting(tempRoot));
            Run("active pane status labels", failures, () => TestActivePaneStatusLabels(tempRoot));
            Run("current pane navigation routing", failures, () => TestCurrentPaneNavigationRouting(tempRoot));
            Run("current pane navigation sources", failures, () => TestCurrentPaneNavigationSources(tempRoot));
            Run("preview properties", failures, () => TestPreviewProperties(tempRoot));
            Run("preview refresh on tab switch", failures, () => TestPreviewRefreshOnTabSwitch(tempRoot));
            Run("preview status states", failures, () => TestPreviewStatusStates(tempRoot));
            Run("address suggestions", failures, () => TestAddressSuggestions(tempRoot));
            Run("xaml wiring checks", failures, TestXamlWiring);
        }
        finally
        {
            TryDelete(tempRoot);
        }

        if (failures.Count == 0)
        {
            Console.WriteLine("Smoke tests passed.");
            return 0;
        }

        Console.Error.WriteLine("Smoke tests failed:");
        foreach (var failure in failures)
        {
            Console.Error.WriteLine($"- {failure}");
        }

        return 1;
    }

    private static void Run(string name, List<string> failures, Action test)
    {
        try
        {
            test();
            Console.WriteLine($"PASS: {name}");
        }
        catch (Exception ex)
        {
            failures.Add($"{name}: {ex.Message}");
            Console.WriteLine($"FAIL: {name}");
        }
    }

    private static void TestRenameFlow(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "rename");
        var original = Path.Combine(folder, "old.txt");
        File.WriteAllText(original, "x");

        vm.LoadDirectory(folder);
        vm.SelectedItem = vm.Items.Single(i => i.Name == "old.txt");

        FileSystemItem? requested = null;
        vm.InlineRenameRequested += (_, item) => requested = item;
        vm.RenameCommand.Execute(null);

        Assert(requested != null, "Rename should raise InlineRenameRequested.");
        vm.RenameItem(requested!, "new.txt");

        Assert(File.Exists(Path.Combine(folder, "new.txt")), "Renamed file should exist.");
        Assert(!File.Exists(original), "Original file should not exist after rename.");
    }

    private static void TestMultiSelectCopy(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "multiselect");
        File.WriteAllText(Path.Combine(folder, "a.txt"), "a");
        File.WriteAllText(Path.Combine(folder, "b.txt"), "b");

        vm.LoadDirectory(folder);
        vm.SelectedItems.Clear();
        vm.SelectedItems.Add(vm.Items.Single(i => i.Name == "a.txt"));
        vm.SelectedItems.Add(vm.Items.Single(i => i.Name == "b.txt"));

        vm.CopyCommand.Execute(null);

        Assert(!clipboard.IsCut, "Copy should set IsCut=false.");
        Assert(clipboard.Paths.Count == 2, "Copy should include all selected items.");
    }

    private static void TestUndoRename(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "undo_rename");
        var original = Path.Combine(folder, "before.txt");
        File.WriteAllText(original, "x");

        vm.LoadDirectory(folder);
        var item = vm.Items.Single(i => i.Name == "before.txt");
        vm.RenameItem(item, "after.txt");

        Assert(File.Exists(Path.Combine(folder, "after.txt")), "Rename should create target file.");
        Assert(vm.CanUndo, "Rename should create undo history.");

        vm.UndoCommand.Execute(null);
        Assert(File.Exists(original), "Undo rename should restore original name.");
    }

    private static void TestRedoRename(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "redo_rename");
        var original = Path.Combine(folder, "before.txt");
        var renamed = Path.Combine(folder, "after.txt");
        File.WriteAllText(original, "x");

        vm.LoadDirectory(folder);
        var item = vm.Items.Single(i => i.Name == "before.txt");
        vm.RenameItem(item, "after.txt");
        vm.UndoCommand.Execute(null);
        vm.RedoCommand.Execute(null);

        Assert(File.Exists(renamed), "Redo rename should reapply the rename.");
        Assert(!File.Exists(original), "Redo rename should remove original file name.");
    }

    private static void TestSelectAllEvent(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "selectall");
        File.WriteAllText(Path.Combine(folder, "one.txt"), "1");
        vm.LoadDirectory(folder);

        var fired = false;
        vm.SelectAllRequested += (_, _) => fired = true;
        vm.SelectAllCommand.Execute(null);

        Assert(fired, "SelectAllRequested should fire.");
    }

    private static void TestDropImport(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var sourceDir = CreateCleanSubdir(root, "drop_src");
        var targetDir = CreateCleanSubdir(root, "drop_dst");

        var copySource = Path.Combine(sourceDir, "copy.txt");
        var moveSource = Path.Combine(sourceDir, "move.txt");
        File.WriteAllText(copySource, "c");
        File.WriteAllText(moveSource, "m");

        vm.LoadDirectory(targetDir);
        vm.ImportDroppedFiles([copySource], targetDir, moveFiles: false);
        Assert(File.Exists(Path.Combine(targetDir, "copy.txt")), "Drop copy should create file in destination.");
        Assert(File.Exists(copySource), "Drop copy should keep source file.");

        vm.ImportDroppedFiles([moveSource], targetDir, moveFiles: true);
        Assert(File.Exists(Path.Combine(targetDir, "move.txt")), "Drop move should create file in destination.");
        Assert(!File.Exists(moveSource), "Drop move should remove source file.");
    }

    private static void TestCreateFolderCollision(string root)
    {
        var fs = new FileSystemService();
        var folder = CreateCleanSubdir(root, "newfolder");

        var first = fs.CreateFolder(folder, "New folder");
        var second = fs.CreateFolder(folder, "New folder");

        Assert(Directory.Exists(first), "First folder should be created.");
        Assert(Directory.Exists(second), "Second folder should be created.");
        Assert(!string.Equals(first, second, StringComparison.OrdinalIgnoreCase), "Second folder should get collision suffix.");
    }

    private static void TestUndoMove(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var sourceDir = CreateCleanSubdir(root, "undo_move_src");
        var targetDir = CreateCleanSubdir(root, "undo_move_dst");

        var source = Path.Combine(sourceDir, "file.txt");
        File.WriteAllText(source, "m");

        vm.LoadDirectory(targetDir);
        vm.ImportDroppedFiles([source], targetDir, moveFiles: true);

        var moved = Path.Combine(targetDir, "file.txt");
        Assert(File.Exists(moved), "Move should place file in destination.");
        Assert(!File.Exists(source), "Move should remove original.");
        Assert(vm.CanUndo, "Move should create undo history.");

        vm.UndoCommand.Execute(null);
        Assert(File.Exists(source), "Undo move should return file to source.");
    }

    private static void TestUndoCopy(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var sourceDir = CreateCleanSubdir(root, "undo_copy_src");
        var targetDir = CreateCleanSubdir(root, "undo_copy_dst");

        var source = Path.Combine(sourceDir, "file.txt");
        File.WriteAllText(source, "c");

        vm.LoadDirectory(targetDir);
        vm.ImportDroppedFiles([source], targetDir, moveFiles: false);

        var copied = Path.Combine(targetDir, "file.txt");
        Assert(File.Exists(copied), "Copy should place file in destination.");
        Assert(File.Exists(source), "Copy should keep source.");
        Assert(vm.CanUndo, "Copy should create undo history.");
        Assert(vm.UndoDescription.Contains("Copy", StringComparison.OrdinalIgnoreCase), "Undo label should describe copy.");

        vm.UndoCommand.Execute(null);
        Assert(!File.Exists(copied), "Undo copy should remove destination copy.");
        Assert(File.Exists(source), "Undo copy should preserve source.");
    }

    private static void TestUndoPermanentDelete(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "undo_permanent_delete");

        var path = Path.Combine(folder, "delete_me.txt");
        File.WriteAllText(path, "d");
        vm.LoadDirectory(folder);

        vm.DeletePaths([path], permanentDelete: true, promptUser: false);
        Assert(!File.Exists(path), "Permanent delete should remove file immediately.");
        Assert(vm.CanUndo, "Permanent delete should create undo history.");
        Assert(vm.UndoDescription.Contains("Permanent Delete", StringComparison.OrdinalIgnoreCase),
            "Undo label should describe permanent delete.");

        vm.UndoCommand.Execute(null);
        Assert(File.Exists(path), "Undo permanent delete should restore file.");
    }

    private static void TestClearHistory(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "clear_history");
        var path = Path.Combine(folder, "x.txt");
        File.WriteAllText(path, "x");

        vm.LoadDirectory(folder);
        vm.RenameItem(vm.Items.Single(i => i.Name == "x.txt"), "y.txt");
        Assert(vm.CanUndo, "History should exist before clear.");

        vm.ClearHistoryCommand.Execute(null);
        Assert(!vm.CanUndo, "Clear history should disable undo.");
        Assert(!vm.CanRedo, "Clear history should disable redo.");
    }

    private static void TestShellIconService(string root)
    {
        var svc = new ShellIconService();
        var folder = CreateCleanSubdir(root, "icons");
        var file = Path.Combine(folder, "x.txt");
        File.WriteAllText(file, "icon");

        var dirIcon = svc.GetIcon(folder, FileSystemItemType.Directory);
        var fileIcon = svc.GetIcon(file, FileSystemItemType.File);

        Assert(dirIcon != null, "Directory icon should be resolved.");
        Assert(fileIcon != null, "File icon should be resolved.");
    }

    private static void TestMoveSameFolderNoOp(string root)
    {
        var fs = new FileSystemService();
        var folder = CreateCleanSubdir(root, "same_folder_move");
        var file = Path.Combine(folder, "same.txt");
        File.WriteAllText(file, "z");

        var results = fs.MoveFiles([file], folder);
        Assert(results.Count == 0, "Same-folder move should be ignored.");
        Assert(File.Exists(file), "Source file should remain unchanged.");
        Assert(!File.Exists(Path.Combine(folder, "same (2).txt")), "No renamed duplicate should be created.");
    }

    private static void TestRedoCopy(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var sourceDir = CreateCleanSubdir(root, "redo_copy_src");
        var targetDir = CreateCleanSubdir(root, "redo_copy_dst");

        var source = Path.Combine(sourceDir, "file.txt");
        File.WriteAllText(source, "rc");

        vm.LoadDirectory(targetDir);
        vm.ImportDroppedFiles([source], targetDir, moveFiles: false);

        var copied = Path.Combine(targetDir, "file.txt");
        Assert(File.Exists(copied), "Copy should place file.");

        vm.UndoCommand.Execute(null);
        Assert(!File.Exists(copied), "Undo should remove copy.");
        Assert(vm.CanRedo, "Redo should be available after undo copy.");

        vm.RedoCommand.Execute(null);
        Assert(File.Exists(copied), "Redo copy should recreate the file.");
    }

    private static void TestRedoMove(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var sourceDir = CreateCleanSubdir(root, "redo_move_src");
        var targetDir = CreateCleanSubdir(root, "redo_move_dst");

        var source = Path.Combine(sourceDir, "file.txt");
        File.WriteAllText(source, "rm");

        vm.LoadDirectory(targetDir);
        vm.ImportDroppedFiles([source], targetDir, moveFiles: true);

        var moved = Path.Combine(targetDir, "file.txt");
        Assert(File.Exists(moved), "Move should place file.");
        Assert(!File.Exists(source), "Move should remove source.");

        vm.UndoCommand.Execute(null);
        Assert(File.Exists(source), "Undo should return file.");
        Assert(vm.CanRedo, "Redo should be available after undo move.");

        vm.RedoCommand.Execute(null);
        Assert(File.Exists(moved), "Redo move should place file again.");
        Assert(!File.Exists(source), "Redo move should remove source again.");
    }

    private static void TestViewModeProperty(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);

        Assert(vm.ViewMode == "Details", "Default view mode should be Details.");

        string? firedMode = null;
        vm.ViewModeChanged += (_, mode) => firedMode = mode;
        vm.ViewMode = "Tiles";

        Assert(vm.ViewMode == "Tiles", "ViewMode should update.");
        Assert(firedMode == "Tiles", "ViewModeChanged event should fire.");
    }

    private static void TestSelectionStatus(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "selection_status");
        File.WriteAllText(Path.Combine(folder, "a.txt"), "a");
        File.WriteAllText(Path.Combine(folder, "b.txt"), "b");

        vm.LoadDirectory(folder);
        Assert(vm.StatusText == "2 items", "Status should show item count.");

        vm.SelectedItems.Add(vm.Items[0]);
        vm.UpdateSelectionStatus();
        Assert(vm.StatusText.Contains("1 selected"), "Status should show selection count.");

        vm.SelectedItems.Add(vm.Items[1]);
        vm.UpdateSelectionStatus();
        Assert(vm.StatusText.Contains("2 selected"), "Status should update for multi-select.");

        vm.SelectedItems.Clear();
        vm.UpdateSelectionStatus();
        Assert(vm.StatusText == "2 items", "Status should reset when selection cleared.");
    }

    private static void TestFilterText(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "filter");
        File.WriteAllText(Path.Combine(folder, "alpha.txt"), "a");
        File.WriteAllText(Path.Combine(folder, "beta.txt"), "b");
        File.WriteAllText(Path.Combine(folder, "gamma.log"), "g");

        vm.LoadDirectory(folder);
        Assert(vm.Items.Count == 3, "Should show all 3 files.");

        vm.FilterText = "alpha";
        Assert(vm.Items.Count == 1, "Filter should show only matching file.");
        Assert(vm.Items[0].Name == "alpha.txt", "Filtered item should be alpha.txt.");

        vm.FilterText = ".txt";
        Assert(vm.Items.Count == 2, "Filter by extension should show 2 files.");

        vm.FilterText = "";
        Assert(vm.Items.Count == 3, "Clearing filter should show all files.");
    }

    private static void TestPropertiesCommand(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "properties");
        File.WriteAllText(Path.Combine(folder, "test.txt"), "hello");

        vm.LoadDirectory(folder);
        vm.SelectedItems.Add(vm.Items.Single(i => i.Name == "test.txt"));

        FileSystemItem? requested = null;
        vm.PropertiesRequested += (_, item) => requested = item;
        vm.ShowPropertiesCommand.Execute(null);

        Assert(requested != null, "Properties command should raise PropertiesRequested event.");
        Assert(requested!.Name == "test.txt", "Properties should be for selected item.");
    }

    private static void TestDuplicateTab(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "dup_tab");

        var bookmarkFile = Path.Combine(root, "dup_bookmarks.json");
        Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", bookmarkFile);
        try
        {
            var vm = new MainViewModel(fs, clipboard);
            vm.NewTabCommand.Execute(null);
            vm.ActiveTab!.NavigateTo(folder);

            var originalTab = vm.ActiveTab;
            vm.DuplicateTab(originalTab);

            Assert(vm.Tabs.Count == 2, "Duplicate should add a second tab.");
            Assert(vm.ActiveTab != originalTab, "Active tab should be the new duplicate.");
            Assert(vm.ActiveTab!.CurrentPath == folder, "Duplicate tab should navigate to same path.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", null);
        }
    }

    private static void TestCloseOtherTabs(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();

        var bookmarkFile = Path.Combine(root, "close_others_bookmarks.json");
        Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", bookmarkFile);
        try
        {
            var vm = new MainViewModel(fs, clipboard);
            vm.NewTabCommand.Execute(null);
            vm.NewTabCommand.Execute(null);
            vm.NewTabCommand.Execute(null);
            Assert(vm.Tabs.Count == 3, "Should have 3 tabs.");

            var keepTab = vm.Tabs[1];
            vm.CloseOtherTabs(keepTab);

            Assert(vm.Tabs.Count == 1, "Close others should leave 1 tab.");
            Assert(vm.ActiveTab == keepTab, "Remaining tab should be the kept one.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", null);
        }
    }

    private static void TestSelectionSizeStatus(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "size_status");
        File.WriteAllText(Path.Combine(folder, "small.txt"), new string('x', 500));
        File.WriteAllText(Path.Combine(folder, "big.txt"), new string('y', 2000));

        vm.LoadDirectory(folder);
        vm.SelectedItems.Add(vm.Items.Single(i => i.Name == "small.txt"));
        vm.SelectedItems.Add(vm.Items.Single(i => i.Name == "big.txt"));
        vm.UpdateSelectionStatus();

        Assert(vm.StatusText.Contains("2 selected"), "Should show 2 selected.");
        Assert(vm.StatusText.Contains("B"), "Should show size with B unit.");
    }

    private static void TestBreadcrumbSegments(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();

        var bookmarkFile = Path.Combine(root, "breadcrumb_bookmarks.json");
        Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", bookmarkFile);
        try
        {
            var vm = new MainViewModel(fs, clipboard);
            vm.NewTabCommand.Execute(null);

            var folder = CreateCleanSubdir(root, "bread");
            var sub = CreateCleanSubdir(folder, "crumb");
            vm.NavigateToPath(sub);

            Assert(vm.BreadcrumbSegments.Count >= 2, "Breadcrumb should have at least 2 segments.");
            var last = vm.BreadcrumbSegments[^1];
            Assert(last.DisplayName == "crumb", "Last breadcrumb segment should be folder name.");
            Assert(last.FullPath == sub, "Last breadcrumb segment path should match.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", null);
        }
    }

    private static void TestRecentFolders(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();

        var bookmarkFile = Path.Combine(root, "recent_bookmarks.json");
        Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", bookmarkFile);
        try
        {
            var vm = new MainViewModel(fs, clipboard);
            vm.NewTabCommand.Execute(null);

            var folder1 = CreateCleanSubdir(root, "recent1");
            var folder2 = CreateCleanSubdir(root, "recent2");
            vm.NavigateToPath(folder1);
            vm.NavigateToPath(folder2);

            Assert(vm.RecentFolders.Count >= 2, "Recent folders should track visited paths.");
            Assert(vm.RecentFolders[0].FullPath == folder2, "Most recent folder should be first.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", null);
        }
    }

    private static void TestSearchInFolder(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "search_root");
        var sub = CreateCleanSubdir(folder, "sub");
        File.WriteAllText(Path.Combine(folder, "match.txt"), "a");
        File.WriteAllText(Path.Combine(folder, "other.log"), "b");
        File.WriteAllText(Path.Combine(sub, "deep_match.txt"), "c");

        vm.LoadDirectory(folder);
        Assert(!vm.IsSearchVisible, "Search should not be visible by default.");

        vm.SearchInFolderCommand.Execute(null);
        Assert(vm.IsSearchVisible, "SearchInFolder should show search bar.");

        vm.SearchText = "match";
        vm.ExecuteSearchSync();

        Assert(vm.IsShowingSearchResults, "Should be showing search results.");
        Assert(vm.Items.Count == 2, "Search should find 2 matching items (match.txt + deep_match.txt).");
        Assert(vm.SearchResultsText.Contains("match"), "Search results text should contain query.");
    }

    private static void TestSearchCloseAndClear(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "search_clear");
        File.WriteAllText(Path.Combine(folder, "a.txt"), "a");

        vm.LoadDirectory(folder);
        vm.SearchInFolderCommand.Execute(null);
        vm.SearchText = "xyz";
        vm.ExecuteSearchSync();

        Assert(vm.IsShowingSearchResults, "Should show search results.");

        vm.ClearSearchResultsCommand.Execute(null);
        Assert(!vm.IsShowingSearchResults, "ClearSearchResults should hide banner.");
        Assert(vm.Items.Count == 1, "Should reload original directory contents.");

        vm.SearchInFolderCommand.Execute(null);
        vm.CloseSearchCommand.Execute(null);
        Assert(!vm.IsSearchVisible, "CloseSearch should hide search bar.");
    }

    private static void TestDualPaneToggle(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "dual_pane");

        var vm = new MainViewModel(fs, clipboard);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(folder);

        var eventCount = 0;
        vm.DualPaneModeChanged += (_, _) => eventCount++;

        vm.ToggleDualPaneCommand.Execute(null);

        Assert(vm.IsDualPaneMode, "ToggleDualPane should enable dual pane mode.");
        var rightPaneTab = vm.RightPaneTab;
        Assert(rightPaneTab != null, "Dual pane mode should create a right pane tab.");
        Assert(rightPaneTab!.CurrentPath == folder, "Right pane should initialize to the active tab path.");
        Assert(vm.RightPaneAddressText == folder, "Right pane address text should reflect the right pane path.");
        Assert(eventCount == 1, "DualPaneModeChanged should fire when enabling dual pane mode.");

        vm.ToggleDualPaneCommand.Execute(null);

        Assert(!vm.IsDualPaneMode, "Second toggle should disable dual pane mode.");
        Assert(eventCount == 2, "DualPaneModeChanged should fire when disabling dual pane mode.");
    }

    private static void TestActivePaneCommandRouting(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var leftFolder = CreateCleanSubdir(root, "active_left");
        var rightFolder = CreateCleanSubdir(root, "active_right");
        var rightSubFolder = CreateCleanSubdir(rightFolder, "sub");
        var rightFile = Path.Combine(rightFolder, "right.txt");
        var leftFile = Path.Combine(leftFolder, "left.txt");
        File.WriteAllText(rightFile, "right");
        File.WriteAllText(leftFile, "left");

        var vm = new MainViewModel(fs, clipboard);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(leftFolder);
        vm.ToggleDualPaneCommand.Execute(null);
        vm.RightPaneTab!.NavigateTo(rightFolder);
        vm.ActivateRightPane();

        var rightItem = vm.RightPaneTab.FileList.Items.Single(i => i.Name == "right.txt");
        vm.RightPaneTab.FileList.SelectedItem = rightItem;
        vm.RightPaneTab.FileList.SelectedItems.Clear();
        vm.RightPaneTab.FileList.SelectedItems.Add(rightItem);

        vm.CopyCommand.Execute(null);
        Assert(clipboard.Paths.Count == 1 && clipboard.Paths[0] == rightFile,
            "Copy should target the active right pane selection.");

        clipboard.SetFiles([leftFile], isCut: false);
        vm.PasteCommand.Execute(null);
        Assert(File.Exists(Path.Combine(rightFolder, "left.txt")),
            "Paste should target the active right pane directory.");

        vm.RightPaneTab.NavigateTo(rightSubFolder);
        vm.NavigateUpCommand.Execute(null);
        Assert(vm.RightPaneTab.CurrentPath == rightFolder,
            "NavigateUp should target the active right pane tab.");
        Assert(vm.CurrentPanePath == rightFolder,
            "CurrentPanePath should reflect the active pane path.");
    }

    private static void TestActivePaneUiCommandRouting(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var leftFolder = CreateCleanSubdir(root, "ui_left");
        var rightFolder = CreateCleanSubdir(root, "ui_right");
        File.WriteAllText(Path.Combine(rightFolder, "rename.txt"), "rename");

        var vm = new MainViewModel(fs, clipboard);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(leftFolder);
        vm.ToggleDualPaneCommand.Execute(null);
        vm.RightPaneTab!.NavigateTo(rightFolder);
        vm.ActivateRightPane();

        var item = vm.RightPaneTab.FileList.Items.Single(i => i.Name == "rename.txt");
        vm.RightPaneTab.FileList.SelectedItem = item;
        vm.RightPaneTab.FileList.SelectedItems.Clear();
        vm.RightPaneTab.FileList.SelectedItems.Add(item);

        var renameRequested = false;
        var selectAllRequested = false;
        var propertiesRequested = false;
        vm.RightPaneTab.FileList.InlineRenameRequested += (_, requestedItem) =>
            renameRequested = requestedItem.Name == "rename.txt";
        vm.RightPaneTab.FileList.SelectAllRequested += (_, _) => selectAllRequested = true;
        vm.RightPaneTab.FileList.PropertiesRequested += (_, requestedItem) =>
            propertiesRequested = requestedItem.Name == "rename.txt";

        vm.RenameCommand.Execute(null);
        vm.SelectAllCommand.Execute(null);
        vm.ShowPropertiesCommand.Execute(null);

        Assert(renameRequested, "Rename should target the active right pane.");
        Assert(selectAllRequested, "SelectAll should target the active right pane.");
        Assert(propertiesRequested, "Properties should target the active right pane.");
    }

    private static void TestActivePaneViewSearchRouting(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var leftFolder = CreateCleanSubdir(root, "view_left");
        var rightFolder = CreateCleanSubdir(root, "view_right");
        File.WriteAllText(Path.Combine(leftFolder, "left.txt"), "left");
        File.WriteAllText(Path.Combine(rightFolder, "alpha.txt"), "a");
        File.WriteAllText(Path.Combine(rightFolder, "beta.log"), "b");
        CreateCleanSubdir(rightFolder, "search-hit");

        var vm = new MainViewModel(fs, clipboard);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(leftFolder);
        vm.ToggleDualPaneCommand.Execute(null);
        vm.RightPaneTab!.NavigateTo(rightFolder);
        vm.ActivateRightPane();

        vm.CurrentFilterText = "alpha";
        Assert(vm.RightPaneTab.FileList.FilterText == "alpha", "Filter text should target the active right pane.");
        Assert(vm.RightPaneTab.FileList.Items.Count == 1, "Right pane filter should narrow right pane items.");

        vm.CurrentViewMode = "Tiles";
        Assert(vm.RightPaneTab.FileList.ViewMode == "Tiles", "View mode should target the active right pane.");

        vm.SearchInFolderCommand.Execute(null);
        Assert(vm.RightPaneTab.FileList.IsSearchVisible, "Search should open for the active right pane.");

        vm.CurrentSearchText = "search";
        vm.ExecuteSearchCommand.Execute(null);
        Assert(vm.RightPaneTab.FileList.IsShowingSearchResults, "Search results should be shown in the active right pane.");
        Assert(vm.RightPaneTab.FileList.Items.Any(i => i.Name == "search-hit"), "Right pane search should search the right pane tree.");

        vm.ClearSearchResultsCommand.Execute(null);
        Assert(!vm.RightPaneTab.FileList.IsShowingSearchResults, "Clearing search should target the active right pane.");
        Assert(vm.CurrentViewMode == "Tiles", "Current view mode should reflect the active pane.");
    }

    private static void TestActivePaneStatusLabels(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var leftFolder = CreateCleanSubdir(root, "status_left");
        var rightFolder = CreateCleanSubdir(root, "status_right");
        File.WriteAllText(Path.Combine(leftFolder, "left.txt"), "left");
        File.WriteAllText(Path.Combine(rightFolder, "right.txt"), "right");

        var vm = new MainViewModel(fs, clipboard);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(leftFolder);

        Assert(vm.CurrentPaneLabel == "Left Pane", "Left pane should be the default active pane label.");
        Assert(vm.LeftPaneStatusText.Contains("items"), "Left pane status should track left pane file list state.");

        vm.ToggleDualPaneCommand.Execute(null);
        vm.RightPaneTab!.NavigateTo(rightFolder);
        vm.ActivateRightPane();

        Assert(vm.CurrentPaneLabel == "Right Pane", "Current pane label should switch with right pane activation.");
        Assert(vm.RightPaneStatusText.Contains("items"), "Right pane status should track right pane file list state.");
    }

    private static void TestCurrentPaneNavigationRouting(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var leftFolder = CreateCleanSubdir(root, "nav_left");
        var rightFolder = CreateCleanSubdir(root, "nav_right");
        var targetFolder = CreateCleanSubdir(root, "nav_target");

        var vm = new MainViewModel(fs, clipboard);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(leftFolder);
        vm.ToggleDualPaneCommand.Execute(null);
        vm.RightPaneTab!.NavigateTo(rightFolder);
        vm.ActivateRightPane();

        vm.NavigateCurrentPaneToPath(targetFolder);

        Assert(vm.RightPaneTab.CurrentPath == targetFolder, "Current-pane navigation should target the active right pane.");
        Assert(vm.ActiveTab!.CurrentPath == leftFolder, "Current-pane navigation should not move the left pane.");
    }

    private static void TestCurrentPaneNavigationSources(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var leftFolder = CreateCleanSubdir(root, "nav_source_left");
        var rightFolder = CreateCleanSubdir(root, "nav_source_right");
        var treeTarget = CreateCleanSubdir(root, "nav_tree_target");
        var bookmarkTarget = CreateCleanSubdir(root, "nav_bookmark_target");

        var vm = new MainViewModel(fs, clipboard);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(leftFolder);
        vm.ToggleDualPaneCommand.Execute(null);
        vm.RightPaneTab!.NavigateTo(rightFolder);
        vm.ActivateRightPane();

        var treeNode = new FolderTreeNodeViewModel(fs)
        {
            Name = Path.GetFileName(treeTarget),
            FullPath = treeTarget
        };

        vm.SelectTreeNode(treeNode);
        Assert(vm.RightPaneTab.CurrentPath == treeTarget, "Tree-node navigation should target the active right pane.");
        Assert(vm.ActiveTab!.CurrentPath == leftFolder, "Tree-node navigation should not move the left pane.");

        var bookmark = new BookmarkItem
        {
            Name = "Bookmark Target",
            Path = bookmarkTarget,
            IsFolder = true
        };

        vm.NavigateBookmarkCommand.Execute(bookmark);
        Assert(vm.RightPaneTab.CurrentPath == bookmarkTarget, "Bookmark navigation should target the active right pane.");
        Assert(vm.ActiveTab.CurrentPath == leftFolder, "Bookmark navigation should still leave the left pane unchanged.");
    }

    private static void TestPreviewProperties(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "preview");
        var textFile = Path.Combine(folder, "preview.txt");
        File.WriteAllLines(textFile, ["line one", "line two"]);

        var vm = new MainViewModel(fs, clipboard);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(folder);

        var previewEvents = 0;
        vm.PreviewModeChanged += (_, _) => previewEvents++;

        var item = vm.ActiveTab!.FileList.Items.Single(i => i.Name == "preview.txt");
        vm.ActiveTab.FileList.SelectedItem = item;
        vm.TogglePreviewCommand.Execute(null);

        Assert(vm.IsPreviewVisible, "TogglePreview should enable preview mode.");
        Assert(previewEvents == 1, "PreviewModeChanged should fire when enabling preview mode.");
        Assert(vm.PreviewFileName == "preview.txt", "Preview should expose the selected file name.");
        Assert(vm.PreviewFileInfo.Contains("txt", StringComparison.OrdinalIgnoreCase), "Preview should include file type information.");
        WaitFor(() => vm.HasPreviewText);
        Assert(vm.HasPreviewText, "Text files should produce text preview content.");
        Assert(!vm.HasPreviewImage, "Text files should not produce image preview content.");
        Assert(vm.PreviewText.Contains("line one"), "Preview should include file contents.");

        vm.TogglePreviewCommand.Execute(null);

        Assert(!vm.IsPreviewVisible, "Second toggle should disable preview mode.");
        Assert(previewEvents == 2, "PreviewModeChanged should fire when disabling preview mode.");
    }

    private static void TestPreviewStatusStates(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "preview_states");
        var unsupportedFile = Path.Combine(folder, "archive.bin");
        var largeTextFile = Path.Combine(folder, "large.txt");
        File.WriteAllBytes(unsupportedFile, [1, 2, 3, 4]);
        File.WriteAllText(largeTextFile, new string('a', 1024 * 1024 + 32));

        var vm = new MainViewModel(fs, clipboard);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(folder);
        vm.TogglePreviewCommand.Execute(null);

        Assert(vm.HasPreviewStatus, "Preview should show a status message when nothing is selected.");
        Assert(vm.PreviewStatusText.Contains("Select"), "Empty preview should prompt for a selection.");

        var unsupportedItem = vm.ActiveTab!.FileList.Items.Single(i => i.Name == "archive.bin");
        vm.ActiveTab.FileList.SelectedItem = unsupportedItem;
        Assert(vm.HasPreviewStatus, "Unsupported files should show a preview status.");
        Assert(vm.PreviewStatusText.Contains("Created") || vm.PreviewStatusText.Contains("Modified"),
            "Unsupported files should show file metadata.");

        var largeTextItem = vm.ActiveTab.FileList.Items.Single(i => i.Name == "large.txt");
        vm.ActiveTab.FileList.SelectedItem = largeTextItem;
        Assert(vm.PreviewStatusText.Contains("1 MB"), "Large text files should show the preview size limit message.");
    }

    private static void TestAddressSuggestions(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "autocomplete");
        var alpha = CreateCleanSubdir(folder, "alpha");
        CreateCleanSubdir(folder, "beta");

        var vm = new MainViewModel(fs, clipboard)
        {
            AddressBarText = Path.Combine(folder, "al")
        };

        vm.UpdateAddressSuggestions();
        WaitFor(() => vm.AddressSuggestions.Count > 0);

        Assert(vm.IsAddressSuggestionsOpen, "Matching folders should open address suggestions.");
        Assert(vm.AddressSuggestions.Count == 1, "Suggestions should be filtered by the typed prefix.");
        Assert(vm.AddressSuggestions[0] == alpha, "Suggestions should contain the matching folder path.");

        vm.AddressBarText = Path.Combine(folder, "zzz");
        vm.UpdateAddressSuggestions();
        WaitFor(() => !vm.IsAddressSuggestionsOpen, 500);

        Assert(!vm.IsAddressSuggestionsOpen, "Non-matching prefixes should close address suggestions.");
        Assert(vm.AddressSuggestions.Count == 0, "Non-matching prefixes should clear suggestions.");
    }

    private static void TestPreviewRefreshOnTabSwitch(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder1 = CreateCleanSubdir(root, "preview_tab_1");
        var folder2 = CreateCleanSubdir(root, "preview_tab_2");
        File.WriteAllText(Path.Combine(folder1, "one.txt"), "from first tab");
        File.WriteAllText(Path.Combine(folder2, "two.txt"), "from second tab");

        var vm = new MainViewModel(fs, clipboard);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(folder1);
        var firstTab = vm.ActiveTab!;
        firstTab.FileList.SelectedItem = firstTab.FileList.Items.Single(i => i.Name == "one.txt");

        vm.TogglePreviewCommand.Execute(null);
        Assert(vm.PreviewFileName == "one.txt", "Preview should reflect the first tab selection.");

        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(folder2);
        var secondTab = vm.ActiveTab!;
        secondTab.FileList.SelectedItem = secondTab.FileList.Items.Single(i => i.Name == "two.txt");

        Assert(vm.PreviewFileName == "two.txt", "Preview should refresh when the active tab changes.");

        vm.ActiveTab = firstTab;
        Assert(vm.PreviewFileName == "one.txt", "Preview should refresh when switching back to another tab.");
    }

    private static void TestXamlWiring()
    {
        var mainXaml = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CubicAIExplorer", "MainWindow.xaml"));
        var appXaml = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CubicAIExplorer", "App.xaml"));
        var main = File.ReadAllText(mainXaml);
        var app = File.ReadAllText(appXaml);

        Assert(main.Contains("Key=\"F2\""), "F2 keybinding should exist.");
        Assert(main.Contains("Key=\"Z\""), "Ctrl+Z keybinding should exist.");
        Assert(main.Contains("Key=\"Y\""), "Ctrl+Y keybinding should exist.");
        Assert(main.Contains("Ctrl+Shift"), "Ctrl+Shift+N keybinding should exist.");
        Assert(main.Contains("SelectionMode=\"Extended\""), "Extended selection should exist.");
        Assert(main.Contains("AllowDrop=\"True\""), "File list drag/drop should be enabled.");
        Assert(main.Contains("Drop=\"FileList_Drop\""), "File list drop handler should be wired.");
        Assert(main.Contains("ClearHistoryCommand"), "Clear history command should be wired.");
        Assert(main.Contains("BookmarkList_SelectionChanged"), "Bookmark single-click navigation should be wired.");
        Assert(main.Contains("ContextMenuOpening=\"FileList_ContextMenuOpening\""), "Context menu handler should be wired.");
        Assert(main.Contains("StaticResource ShellIconConverter"), "Shell icon converter should be used in MainWindow.");
        Assert(app.Contains("ShellIconConverter"), "ShellIconConverter should be in app resources.");
        Assert(main.Contains("CutCommand"), "Cut toolbar button should be wired.");
        Assert(main.Contains("CopyCommand"), "Copy toolbar button should be wired.");
        Assert(main.Contains("PasteCommand"), "Paste toolbar button should be wired.");
        Assert(main.Contains("RefreshCommand"), "Refresh toolbar button should be wired.");
        Assert(main.Contains("UndoCommand"), "Undo toolbar button should be wired.");
        Assert(main.Contains("RedoCommand"), "Redo toolbar button should be wired.");
        Assert(app.Contains("DetailsNameCellTemplate"), "Details name cell template should be in resources.");
        Assert(app.Contains("ListViewItemTemplate"), "List view template should be in resources.");
        Assert(app.Contains("TileViewItemTemplate"), "Tile view template should be in resources.");
        Assert(main.Contains("FilterTextBox"), "Filter text box should be in MainWindow.");
        Assert(main.Contains("PropertiesMenuItem"), "Properties menu item should be in context menu.");
        Assert(main.Contains("OpenInExplorerMenuItem"), "Open in Explorer menu item should exist.");
        Assert(main.Contains("DuplicateTab_Click"), "Tab duplicate handler should be wired.");
        Assert(main.Contains("CloseOtherTabs_Click"), "Close other tabs handler should be wired.");
        Assert(main.Contains("FolderTree_Drop"), "Folder tree drop handler should be wired.");
        Assert(main.Contains("AllowDrop=\"True\""), "Drop should be enabled.");
        Assert(main.Contains("BreadcrumbSegments"), "Breadcrumb segments binding should exist.");
        Assert(main.Contains("BreadcrumbSegment_Click"), "Breadcrumb click handler should be wired.");
        Assert(main.Contains("RecentFolders"), "Recent folders binding should exist.");
        Assert(main.Contains("SearchTextBox"), "Search text box should exist.");
        Assert(main.Contains("SearchInFolderCommand"), "Search command binding should exist.");
        Assert(main.Contains("Command=\"{Binding CopyCommand}\""), "Copy bindings should route through the main view model.");
        Assert(main.Contains("Command=\"{Binding GoBackCommand}\""), "Back bindings should route through the main view model.");
        Assert(main.Contains("Command=\"{Binding RenameCommand}\""), "Rename bindings should route through the main view model.");
        Assert(main.Contains("Command=\"{Binding SelectAllCommand}\""), "Select-all bindings should route through the main view model.");
        Assert(main.Contains("Command=\"{Binding SearchInFolderCommand}\""), "Search bindings should route through the main view model.");
        Assert(main.Contains("CurrentFilterText"), "Filter bar should bind to the current active pane.");
        Assert(main.Contains("CurrentSearchText"), "Search bar should bind to the current active pane.");
        Assert(main.Contains("CurrentIsSearchVisible"), "Search visibility should bind to the current active pane.");
        Assert(main.Contains("CurrentPaneLabel"), "Status bar should include the current pane label.");
        Assert(main.Contains("RightPaneHeader_MouseLeftButtonDown"), "Right pane header activation should be wired.");
        Assert(main.Contains("RightPaneAddressBox"), "Right pane should expose an inline address editor.");
        Assert(main.Contains("RightPaneAddressGo_Click"), "Right pane address editor should wire its go action.");
        Assert(main.Contains("RightPaneStatusText"), "Right pane header/status text should be bound.");
        Assert(main.Contains("PreviewStatusText"), "Preview panel should bind a fallback status message.");
        Assert(main.Contains("CurrentPanePath"), "Status bar should reflect the current active pane path.");
        Assert(main.Contains("ContextMenuOpening=\"RightPane_ContextMenuOpening\""), "Right pane context menu handler should be wired.");
        Assert(main.Contains("GridViewColumnHeader.Click=\"RightPaneHeader_Click\""), "Right pane sort handler should be wired.");
        Assert(main.Contains("GotKeyboardFocus=\"RightPane_GotKeyboardFocus\""), "Right pane focus tracking should be wired.");
        Assert(main.Contains("ToggleDualPaneCommand"), "Dual pane menu should invoke the dual pane command.");
        Assert(main.Contains("TogglePreviewCommand"), "Preview menu should invoke the preview command.");
        Assert(app.Contains("IconBack"), "Vector icon resources should exist.");
        Assert(app.Contains("IconSearch"), "Search icon resource should exist.");
        Assert(!main.Contains("&#x25C0;"), "Unicode arrow symbols should be replaced with vector icons.");
    }

    private static void TestBookmarks(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "bookmarks");
        var bookmarkFile = Path.Combine(root, "bookmarks.json");
        Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", bookmarkFile);
        TryDelete(bookmarkFile);

        try
        {
            var vm = new MainViewModel(fs, clipboard);
            vm.NewTabCommand.Execute(null);
            vm.NavigateToPath(folder);

            var before = vm.Bookmarks.Count;
            vm.AddBookmarkCommand.Execute(null);
            var afterFirstAdd = vm.Bookmarks.Count;
            vm.AddBookmarkCommand.Execute(null);
            var afterSecondAdd = vm.Bookmarks.Count;

            Assert(afterFirstAdd == before + 1, "AddBookmark should add current folder.");
            Assert(afterSecondAdd == afterFirstAdd, "AddBookmark should ignore duplicates.");
            Assert(File.Exists(bookmarkFile), "Bookmark file should be created.");

            var vmReloaded = new MainViewModel(fs, clipboard);
            Assert(vmReloaded.Bookmarks.Any(b => string.Equals(b.Path, folder, StringComparison.OrdinalIgnoreCase)),
                "Bookmarks should persist across MainViewModel instances.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", null);
        }
    }

    private static string CreateCleanSubdir(string root, string name)
    {
        var path = Path.Combine(root, name);
        TryDelete(path);
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);

            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // ignore cleanup failures in temp paths
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void WaitFor(Func<bool> condition, int timeoutMs = 2000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (!condition() && Environment.TickCount64 < deadline)
            Thread.Sleep(50);
    }

    private sealed class FakeClipboardService : IClipboardService
    {
        public IReadOnlyList<string> Paths { get; private set; } = [];
        public bool IsCut { get; private set; }

        public void SetFiles(IEnumerable<string> paths, bool isCut)
        {
            Paths = paths.ToArray();
            IsCut = isCut;
        }

        public (IReadOnlyList<string> Paths, bool IsCut) GetFiles() => (Paths, IsCut);

        public bool HasFiles() => Paths.Count > 0;

        public void Clear()
        {
            Paths = [];
            IsCut = false;
        }
    }
}
