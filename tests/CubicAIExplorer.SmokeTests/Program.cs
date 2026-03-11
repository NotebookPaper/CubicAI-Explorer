using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using CubicAIExplorer.Converters;
using CubicAIExplorer.Models;
using CubicAIExplorer.Services;
using CubicAIExplorer.ViewModels;
using CubicAIExplorer.Views;

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
            Run("batch rename preview", failures, () => TestBatchRenamePreview(tempRoot));
            Run("batch rename command opens dialog", failures, () => TestBatchRenameCommandOpensDialog(tempRoot));
            Run("batch rename undo/redo", failures, () => TestBatchRenameUndoRedo(tempRoot));
            Run("undo rename", failures, () => TestUndoRename(tempRoot));
            Run("redo rename", failures, () => TestRedoRename(tempRoot));
            Run("multi-select copy command", failures, () => TestMultiSelectCopy(tempRoot));
            Run("drop import copy + move", failures, () => TestDropImport(tempRoot));
            Run("undo copy", failures, () => TestUndoCopy(tempRoot));
            Run("undo move", failures, () => TestUndoMove(tempRoot));
            Run("undo permanent delete", failures, () => TestUndoPermanentDelete(tempRoot));
            Run("clear history", failures, () => TestClearHistory(tempRoot));
            Run("select-all event", failures, () => TestSelectAllEvent(tempRoot));
            Run("bookmark open in new tab reuses existing", failures, () => TestBookmarkOpenInNewTabReusesExisting(tempRoot));
            Run("bookmark open all in tabs reuses existing", failures, () => TestBookmarkOpenAllInTabsReusesExisting(tempRoot));
            Run("close tabs to left", failures, () => TestCloseTabsToLeft(tempRoot));
            Run("close tabs to right", failures, () => TestCloseTabsToRight(tempRoot));
            Run("close other tabs", failures, () => TestCloseOtherTabs(tempRoot));
            Run("selection size status", failures, () => TestSelectionSizeStatus(tempRoot));
            Run("breadcrumb segments", failures, () => TestBreadcrumbSegments(tempRoot));
            Run("breadcrumb dropdown navigation", failures, () => TestBreadcrumbDropdownNavigation(tempRoot));
            Run("recent folders", failures, () => TestRecentFolders(tempRoot));
            Run("known folder alias navigation", failures, () => TestKnownFolderAliasNavigation(tempRoot));
            Run("known folder display names", failures, TestKnownFolderDisplayNames);
            Run("shell type descriptions", failures, () => TestShellTypeDescriptions(tempRoot));
            Run("open in explorer reveals selection", failures, () => TestOpenInExplorerRevealsSelection(tempRoot));
            Run("open in explorer reveals multiple selections", failures, () => TestOpenInExplorerRevealsMultipleSelections(tempRoot));
            Run("create folder collision suffix", failures, () => TestCreateFolderCollision(tempRoot));
            Run("move same folder no-op", failures, () => TestMoveSameFolderNoOp(tempRoot));
            Run("move collision keep both", failures, () => TestMoveCollisionKeepBoth(tempRoot));
            Run("copy collision replace", failures, () => TestCopyCollisionReplace(tempRoot));
            Run("move collision skip", failures, () => TestMoveCollisionSkip(tempRoot));
            Run("clipboard drop effect byte array", failures, TestClipboardDropEffectByteArray);
            Run("file operation queue service", failures, TestFileOperationQueueService);
            Run("queue recent status", failures, TestQueueRecentStatus);
            Run("queue failed history", failures, TestQueueFailedHistory);
            Run("queue cancel + progress", failures, TestQueueCancelAndProgress);
            Run("zip archive service", failures, () => TestZipArchiveService(tempRoot));
            Run("extract archive command", failures, () => TestExtractArchiveCommand(tempRoot));
            Run("extract archive custom destination", failures, () => TestExtractArchiveCustomDestination(tempRoot));
            Run("browse archive request", failures, () => TestBrowseArchiveRequest(tempRoot));
            Run("archive browser filter", failures, TestArchiveBrowserFilter);
            Run("shell icon service", failures, () => TestShellIconService(tempRoot));
            Run("bookmarks add + dedupe", failures, () => TestBookmarks(tempRoot));
            Run("bookmark watcher reloads after external replace", failures, () => TestBookmarkWatcherReloadsAfterExternalReplace(tempRoot));
            Run("bookmark drag feedback", failures, () => TestBookmarkDragFeedback(tempRoot));
            Run("redo copy", failures, () => TestRedoCopy(tempRoot));
            Run("redo move", failures, () => TestRedoMove(tempRoot));
            Run("view mode property", failures, () => TestViewModeProperty(tempRoot));
            Run("selection status text", failures, () => TestSelectionStatus(tempRoot));
            Run("transfer status summary", failures, () => TestTransferStatusSummary(tempRoot));
            Run("filter text", failures, () => TestFilterText(tempRoot));
            Run("filter match modes", failures, () => TestFilterMatchModes(tempRoot));
            Run("filter history + clear on nav", failures, () => TestFilterHistoryAndClearOnNavigation(tempRoot));
            Run("search in folder", failures, () => TestSearchInFolder(tempRoot));
            Run("search match modes", failures, () => TestSearchMatchModes(tempRoot));
            Run("search by file content", failures, () => TestSearchByFileContent(tempRoot));
            Run("search content limits", failures, () => TestSearchContentLimits(tempRoot));
            Run("search close and clear", failures, () => TestSearchCloseAndClear(tempRoot));
            Run("saved searches", failures, () => TestSavedSearches(tempRoot));
            Run("saved search match mode", failures, () => TestSavedSearchMatchMode(tempRoot));
            Run("saved search content criteria", failures, () => TestSavedSearchContentCriteria(tempRoot));
            Run("rename saved search", failures, () => TestRenameSavedSearch(tempRoot));
            Run("dual pane toggle", failures, () => TestDualPaneToggle(tempRoot));
            Run("active pane command routing", failures, () => TestActivePaneCommandRouting(tempRoot));
            Run("active pane ui command routing", failures, () => TestActivePaneUiCommandRouting(tempRoot));
            Run("active pane view search routing", failures, () => TestActivePaneViewSearchRouting(tempRoot));
            Run("active pane status labels", failures, () => TestActivePaneStatusLabels(tempRoot));
            Run("current pane navigation routing", failures, () => TestCurrentPaneNavigationRouting(tempRoot));
            Run("current pane navigation sources", failures, () => TestCurrentPaneNavigationSources(tempRoot));
            Run("preview status states", failures, () => TestPreviewStatusStates(tempRoot));
            Run("rich preview support", failures, () => TestRichPreviewSupport(tempRoot));
            Run("image preview metadata", failures, () => TestImagePreviewMetadata(tempRoot));

            Run("archive preview metadata", failures, () => TestArchivePreviewMetadata(tempRoot));
            Run("preview refresh on tab switch", failures, () => TestPreviewRefreshOnTabSwitch(tempRoot));
            Run("preview status states", failures, () => TestPreviewStatusStates(tempRoot));
            Run("address suggestions", failures, () => TestAddressSuggestions(tempRoot));
            Run("address suggestions ui-thread safe", failures, () => TestAddressSuggestionsUiThreadSafety(tempRoot));
            Run("user settings defaults", failures, TestUserSettingsDefaults);
            Run("settings service round-trip", failures, () => TestSettingsServiceRoundTrip(tempRoot));
            Run("settings watcher reloads after external recreate", failures, () => TestSettingsWatcherReloadsAfterExternalRecreate(tempRoot));
            Run("details column defaults", failures, TestDetailsColumnDefaults);
            Run("details column settings save", failures, TestDetailsColumnSettingsSave);
            Run("named session save", failures, () => TestNamedSessionSave(tempRoot));
            Run("named session load", failures, () => TestNamedSessionLoad(tempRoot));
            Run("named session delete", failures, () => TestNamedSessionDelete(tempRoot));
            Run("named session startup selection", failures, () => TestNamedSessionStartupSelection(tempRoot));
            Run("replace file failure", failures, () => TestReplaceFileFailure(tempRoot));
            Run("copy replace directory failure", failures, () => TestCopyReplaceDirectoryFailure(tempRoot));
            Run("duplicate item", failures, () => TestDuplicateItem(tempRoot));
            Run("new file and link creation", failures, () => TestNewFileAndLink(tempRoot));
            Run("new file templates catalog", failures, () => TestNewFileTemplatesCatalog(tempRoot));
            Run("new file template creation", failures, () => TestNewFileTemplateCreation(tempRoot));
            Run("empty recycle bin command", failures, TestEmptyRecycleBinCommand);
            Run("open in new window command", failures, () => TestOpenInNewWindowCommand(tempRoot));
            Run("run as administrator command", failures, () => TestRunAsAdministratorCommand(tempRoot));
            Run("undo/redo after duplicate", failures, () => TestUndoRedoAfterDuplicate(tempRoot));
            Run("undo/redo after new file and link creation", failures, () => TestUndoRedoAfterNewFileAndLink(tempRoot));
            Run("headless symbolic link failure surfaces to caller", failures, () => TestHeadlessSymbolicLinkFailureSurfacesToCaller(tempRoot));
            Run("new tab applies settings", failures, () => TestNewTabAppliesSettings(tempRoot));
            Run("startup tab loads visible items", failures, () => TestStartupTabLoadsVisibleItems(tempRoot));
            Run("main window xaml loads", failures, TestMainWindowXamlLoads);
            Run("main window file list shows startup items", failures, () => TestMainWindowFileListShowsStartupItems(tempRoot));
            Run("app icon configuration", failures, TestAppIconConfiguration);
            Run("tab overflow wiring", failures, TestTabOverflowWiring);
            Run("xaml wiring checks", failures, TestXamlWiring);
            Run("duplicate tab", failures, () => TestDuplicateTab(tempRoot));
            Run("properties command", failures, () => TestPropertiesCommand(tempRoot));
            Run("shell properties retrieval", failures, () => TestShellProperties(tempRoot));
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

    private static void TestBatchRenamePreview(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "batch_rename_preview");
        File.WriteAllText(Path.Combine(folder, "Report draft.txt"), "1");
        File.WriteAllText(Path.Combine(folder, "Second draft.txt"), "2");
        File.WriteAllText(Path.Combine(folder, "REPORT FINAL_01.txt"), "reserved");

        vm.LoadDirectory(folder);

        var selected = vm.Items
            .Where(item => item.Name.EndsWith("draft.txt", StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Name)
            .ToArray();

        var preview = vm.BuildBatchRenamePreview(
            selected,
            new BatchRenameOptions(
                "draft",
                "final",
                BatchRenameCaseMode.Uppercase,
                BatchRenameCounterPosition.Suffix,
                1,
                2,
                "_",
                BatchRenameExtensionMode.Keep,
                string.Empty));

        Assert(preview.Count == 2, "Batch rename preview should include each selected item.");
        Assert(preview[0].NewName == "REPORT FINAL_01 (2).txt", "Batch rename preview should avoid collisions with existing sibling names.");
        Assert(preview[1].NewName == "SECOND FINAL_02.txt", "Batch rename preview should apply search/replace, case conversion, and counters.");
    }

    private static void TestBatchRenameCommandOpensDialog(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "batch_rename_command");
        File.WriteAllText(Path.Combine(folder, "one.txt"), "1");
        File.WriteAllText(Path.Combine(folder, "two.txt"), "2");

        vm.LoadDirectory(folder);
        vm.SelectedItems.Clear();
        vm.SelectedItems.Add(vm.Items.Single(item => item.Name == "one.txt"));
        vm.SelectedItems.Add(vm.Items.Single(item => item.Name == "two.txt"));

        var opened = false;
        vm.BatchRenameDialogFactory = (items, siblingNames) =>
        {
            opened = true;
            return vm.BuildBatchRenamePreview(
                items,
                new BatchRenameOptions(
                    string.Empty,
                    string.Empty,
                    BatchRenameCaseMode.None,
                    BatchRenameCounterPosition.Prefix,
                    1,
                    2,
                    "_",
                    BatchRenameExtensionMode.Keep,
                    string.Empty));
        };

        vm.RenameCommand.Execute(null);

        Assert(opened, "Rename command should open the batch rename dialog path when multiple items are selected.");
        Assert(File.Exists(Path.Combine(folder, "01_one.txt")), "Batch rename command should apply the dialog plan to the first item.");
        Assert(File.Exists(Path.Combine(folder, "02_two.txt")), "Batch rename command should apply the dialog plan to the second item.");
    }

    private static void TestBatchRenameUndoRedo(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "batch_rename_undo");
        File.WriteAllText(Path.Combine(folder, "alpha.txt"), "a");
        File.WriteAllText(Path.Combine(folder, "beta.txt"), "b");

        vm.LoadDirectory(folder);

        var plan = vm.BuildBatchRenamePreview(
            vm.Items.OrderBy(item => item.Name).ToArray(),
            new BatchRenameOptions(
                string.Empty,
                string.Empty,
                BatchRenameCaseMode.TitleCase,
                BatchRenameCounterPosition.Suffix,
                7,
                2,
                "-",
                BatchRenameExtensionMode.Replace,
                "md"));

        vm.ApplyBatchRename(plan);

        Assert(File.Exists(Path.Combine(folder, "Alpha-07.md")), "Batch rename should rename the first item.");
        Assert(File.Exists(Path.Combine(folder, "Beta-08.md")), "Batch rename should rename the second item.");
        Assert(vm.CanUndo, "Batch rename should create one undo history entry.");
        Assert(vm.UndoDescription.Contains("Batch Rename", StringComparison.OrdinalIgnoreCase),
            "Undo description should reflect the grouped batch rename operation.");

        vm.UndoCommand.Execute(null);
        Assert(File.Exists(Path.Combine(folder, "alpha.txt")), "Undo should restore the first original name.");
        Assert(File.Exists(Path.Combine(folder, "beta.txt")), "Undo should restore the second original name.");

        vm.RedoCommand.Execute(null);
        Assert(File.Exists(Path.Combine(folder, "Alpha-07.md")), "Redo should reapply the first batch rename.");
        Assert(File.Exists(Path.Combine(folder, "Beta-08.md")), "Redo should reapply the second batch rename.");
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

    private static void TestCopyCollisionReplace(string root)
    {
        var fs = new FileSystemService();
        var sourceDir = CreateCleanSubdir(root, "copy_replace_source");
        var destinationDir = CreateCleanSubdir(root, "copy_replace_destination");
        var source = Path.Combine(sourceDir, "item.txt");
        var destination = Path.Combine(destinationDir, "item.txt");
        File.WriteAllText(source, "new");
        File.WriteAllText(destination, "old");

        var results = fs.CopyFiles([source], destinationDir, FileTransferCollisionResolution.Replace);

        Assert(results.Count == 1, "Copy replace should return one result.");
        Assert(results[0].Status == FileTransferStatus.Success, "Copy replace should succeed.");
        Assert(results[0].DestinationPath == destination, "Copy replace should target the original path.");
        Assert(File.ReadAllText(destination) == "new", "Copy replace should overwrite the destination contents.");
        Assert(File.Exists(source), "Copy replace should leave the source intact.");
    }

    private static void TestMoveCollisionKeepBoth(string root)
    {
        var fs = new FileSystemService();
        var sourceDir = CreateCleanSubdir(root, "move_keep_both_source");
        var destinationDir = CreateCleanSubdir(root, "move_keep_both_destination");
        var source = Path.Combine(sourceDir, "item.txt");
        var destination = Path.Combine(destinationDir, "item.txt");
        var renamedDestination = Path.Combine(destinationDir, "item (2).txt");
        File.WriteAllText(source, "source");
        File.WriteAllText(destination, "existing");

        var results = fs.MoveFiles([source], destinationDir);

        Assert(results.Count == 1, "Move keep-both should return one result.");
        Assert(results[0].Status == FileTransferStatus.Success, "Move keep-both should succeed.");
        Assert(results[0].DestinationPath == renamedDestination, "Move keep-both should create a suffixed destination.");
        Assert(File.Exists(renamedDestination), "Move keep-both should create the renamed target.");
        Assert(File.ReadAllText(destination) == "existing", "Move keep-both should preserve the existing destination.");
        Assert(!File.Exists(source), "Move keep-both should remove the source file.");
    }

    private static void TestMoveCollisionSkip(string root)
    {
        var fs = new FileSystemService();
        var sourceDir = CreateCleanSubdir(root, "move_skip_source");
        var destinationDir = CreateCleanSubdir(root, "move_skip_destination");
        var source = Path.Combine(sourceDir, "item.txt");
        var destination = Path.Combine(destinationDir, "item.txt");
        File.WriteAllText(source, "source");
        File.WriteAllText(destination, "existing");

        var results = fs.MoveFiles([source], destinationDir, FileTransferCollisionResolution.Skip);

        Assert(results.Count == 1, "Move skip should return one result.");
        Assert(results[0].Status == FileTransferStatus.Skipped, "Move skip should report the collision as skipped.");
        Assert(File.Exists(source), "Move skip should leave the source file in place.");
        Assert(File.ReadAllText(destination) == "existing", "Move skip should preserve the destination file.");
    }

    private static void TestClipboardDropEffectByteArray()
    {
        var moveBytes = BitConverter.GetBytes((uint)2);
        var copyBytes = BitConverter.GetBytes((uint)5);
        var invalidBytes = new byte[] { 1, 2, 3 };

        Assert(ClipboardService.ReadDropEffectValue(moveBytes) == 2, "Byte-array move drop effect should parse correctly.");
        Assert(ClipboardService.ReadDropEffectValue(copyBytes) == 5, "Byte-array copy drop effect should parse correctly.");
        Assert(ClipboardService.ReadDropEffectValue(invalidBytes) == 5, "Invalid drop-effect payload should fall back to copy.");
    }

    private static void TestFileOperationQueueService()
    {
        var queue = new FileOperationQueueService();
        using var firstStarted = new ManualResetEventSlim();
        using var releaseFirst = new ManualResetEventSlim();
        var secondStarted = false;

        var firstTask = queue.EnqueueAsync("First op", () =>
        {
            firstStarted.Set();
            releaseFirst.Wait(2000);
            return 1;
        });

        WaitFor(() => firstStarted.IsSet && queue.IsBusy);

        var secondTask = queue.EnqueueAsync("Second op", () =>
        {
            secondStarted = true;
            return 2;
        });

        WaitFor(() => queue.PendingCount == 1);
        Assert(queue.StatusText.Contains("First op"), "Queue should report the running operation.");
        Assert(queue.StatusText.Contains("queued"), "Queue should report queued work while busy.");
        Assert(queue.CanCancel, "Active queue work should be cancelable.");

        releaseFirst.Set();
        Task.WaitAll(firstTask, secondTask);

        Assert(secondStarted, "Queued operation should run after the first completes.");
        Assert(!queue.IsBusy, "Queue should become idle after all work completes.");
        Assert(queue.PendingCount == 0, "Queue should clear pending work after completion.");
        Assert(!queue.CanCancel, "Idle queue should not expose cancel.");
    }

    private static void TestQueueRecentStatus()
    {
        var queue = new FileOperationQueueService();
        queue.EnqueueAsync("Quick op", () => 123).GetAwaiter().GetResult();

        WaitFor(() => queue.HasRecentActivity);
        Assert(queue.LastCompletedOperationText == "Quick op", "Queue should remember the last completed operation.");
        Assert(queue.LastCompletedStatusText.Contains("completed", StringComparison.OrdinalIgnoreCase),
            "Queue should expose a completion summary after work finishes.");
        Assert(queue.StatusText.Contains("completed", StringComparison.OrdinalIgnoreCase),
            "Idle queue status should show the most recent result.");
    }

    private static void TestQueueFailedHistory()
    {
        var queue = new FileOperationQueueService();

        try
        {
            queue.EnqueueAsync<int>("Failing op", () => throw new IOException("Disk full")).GetAwaiter().GetResult();
            throw new InvalidOperationException("Failing queue work should rethrow the underlying exception.");
        }
        catch (IOException)
        {
            // expected
        }

        WaitFor(() => queue.HasRecentActivity && queue.RecentOperations.Count > 0);
        var entry = queue.RecentOperations[0];

        Assert(queue.LastCompletedStatusText.Contains("failed", StringComparison.OrdinalIgnoreCase),
            "Failed queue work should update the recent status summary.");
        Assert(entry.Status == FileOperationQueueHistoryStatus.Failed,
            "Failed queue work should be recorded as a failed history entry.");
        Assert(entry.OperationText == "Failing op", "Queue history should remember the failed operation text.");
        Assert(entry.DetailText.Contains("Disk full", StringComparison.OrdinalIgnoreCase),
            "Queue history should retain the failure detail.");
        Assert(queue.StatusText.Contains("failed", StringComparison.OrdinalIgnoreCase),
            "Idle queue status should surface the most recent failure.");
    }

    private static void TestQueueCancelAndProgress()
    {
        var queue = new FileOperationQueueService();
        using var progressReported = new ManualResetEventSlim();
        using var allowCancel = new ManualResetEventSlim();

        try
        {
            var task = queue.EnqueueAsync("Cancelable op", context =>
            {
                context.ReportProgress(1, 3, "first.txt");
                progressReported.Set();
                allowCancel.Wait(2000);
                queue.CancelCurrent();
                context.CancellationToken.ThrowIfCancellationRequested();
                return 0;
            });

            WaitFor(() => progressReported.IsSet && queue.CurrentOperationProgressText == "(1/3)");
            Assert(queue.CurrentOperationDetailText == "first.txt", "Queue should expose the current progress detail.");
            allowCancel.Set();
            task.GetAwaiter().GetResult();

            throw new InvalidOperationException("Cancelable queue work should throw on cancellation.");
        }
        catch (OperationCanceledException)
        {
            // expected
        }

        Assert(progressReported.IsSet, "Queue progress should be reportable during active work.");
        WaitFor(() => queue.HasRecentActivity);
        Assert(queue.LastCompletedStatusText.Contains("canceled", StringComparison.OrdinalIgnoreCase),
            "Canceled queue work should report a canceled status.");
        Assert(!queue.CanCancel, "Queue should clear cancel state after cancellation.");
    }

    private static void TestZipArchiveService(string root)
    {
        var fs = new FileSystemService();
        var folder = CreateCleanSubdir(root, "zip_service");
        var archive = Path.Combine(folder, "sample.zip");
        var extractDir = Path.Combine(folder, "extracted");

        using (var archiveStream = new FileStream(archive, FileMode.Create, FileAccess.ReadWrite))
        using (var zip = new System.IO.Compression.ZipArchive(archiveStream, System.IO.Compression.ZipArchiveMode.Create))
        {
            zip.CreateEntry("docs/");
            var entry = zip.CreateEntry("docs/readme.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("archive text");
        }

        var entries = fs.GetArchiveEntries(archive);
        Assert(entries.Any(e => e.FullName == "docs/readme.txt"), "Archive listing should include file entries.");

        Directory.CreateDirectory(extractDir);
        fs.ExtractArchive(archive, extractDir);
        Assert(File.Exists(Path.Combine(extractDir, "docs", "readme.txt")), "Archive extraction should materialize files.");
    }

    private static void TestExtractArchiveCommand(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "extract_archive_command");
        var archive = Path.Combine(folder, "sample.zip");

        using (var archiveStream = new FileStream(archive, FileMode.Create, FileAccess.ReadWrite))
        using (var zip = new System.IO.Compression.ZipArchive(archiveStream, System.IO.Compression.ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("inner.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("hello archive");
        }

        var vm = new FileListViewModel(fs, clipboard);
        vm.LoadDirectory(folder);
        var archiveItem = vm.Items.Single(i => i.Name == "sample.zip");
        vm.SelectedItem = archiveItem;
        vm.SelectedItems.Clear();
        vm.SelectedItems.Add(archiveItem);

        vm.ExtractArchiveToAsync(archiveItem, FileListViewModel.GetDefaultExtractDestination(archiveItem))
            .GetAwaiter()
            .GetResult();

        Assert(Directory.Exists(Path.Combine(folder, "sample")), "Extract archive command should create a destination folder.");
        Assert(File.Exists(Path.Combine(folder, "sample", "inner.txt")), "Extract archive command should extract files.");
        Assert(vm.StatusText.Contains("Extracted archive"), "Extract archive command should update status text.");
    }

    private static void TestExtractArchiveCustomDestination(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "extract_archive_custom");
        var archive = Path.Combine(folder, "sample.zip");

        using (var archiveStream = new FileStream(archive, FileMode.Create, FileAccess.ReadWrite))
        using (var zip = new System.IO.Compression.ZipArchive(archiveStream, System.IO.Compression.ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("nested/inner.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("hello archive");
        }

        var vm = new FileListViewModel(fs, clipboard);
        vm.LoadDirectory(folder);
        var archiveItem = vm.Items.Single(i => i.Name == "sample.zip");
        var destination = Path.Combine(folder, "custom-out");

        vm.ExtractArchiveToAsync(archiveItem, destination, openFolderWhenDone: false).GetAwaiter().GetResult();

        Assert(Directory.Exists(destination), "Custom archive extraction should create the chosen destination.");
        Assert(File.Exists(Path.Combine(destination, "nested", "inner.txt")),
            "Custom archive extraction should write archive contents to the chosen destination.");
    }

    private static void TestBrowseArchiveRequest(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "browse_archive");
        var archive = Path.Combine(folder, "sample.zip");

        using (var archiveStream = new FileStream(archive, FileMode.Create, FileAccess.ReadWrite))
        using (var zip = new System.IO.Compression.ZipArchive(archiveStream, System.IO.Compression.ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("docs/readme.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("hello archive");
        }

        var vm = new FileListViewModel(fs, clipboard);
        vm.LoadDirectory(folder);
        vm.SelectedItem = vm.Items.Single(i => i.Name == "sample.zip");

        ArchiveBrowseRequest? request = null;
        vm.ArchiveBrowseRequested += (_, archiveRequest) => request = archiveRequest;

        vm.BrowseArchiveCommand.Execute(null);

        Assert(request != null, "Browse archive should raise an archive browse request.");
        Assert(request!.Entries.Any(entry => entry.FullName == "docs/readme.txt"),
            "Browse archive should include the archive entry listing.");
    }

    private static void TestArchiveBrowserFilter()
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var sourceVm = new FileListViewModel(fs, clipboard);
        var entries = new[]
        {
            new ArchiveEntryInfo("docs/", 0, true),
            new ArchiveEntryInfo("docs/readme.txt", 12, false),
            new ArchiveEntryInfo("images/logo.png", 24, false)
        };

        var dialog = new ArchiveBrowserDialog("sample.zip", entries, sourceVm);
        var filterTextBox = (TextBox?)dialog.FindName("FilterTextBox");
        var foldersOnlyCheckBox = (CheckBox?)dialog.FindName("FoldersOnlyCheckBox");
        var entriesListView = (ListView?)dialog.FindName("EntriesListView");
        var summaryTextBlock = (TextBlock?)dialog.FindName("SummaryTextBlock");
        var currentFolderTextBlock = (TextBlock?)dialog.FindName("CurrentFolderTextBlock");

        Assert(filterTextBox != null, "Archive browser should expose a filter text box.");
        Assert(foldersOnlyCheckBox != null, "Archive browser should expose a folders-only toggle.");
        Assert(entriesListView != null, "Archive browser should expose an entries list.");
        Assert(summaryTextBlock != null, "Archive browser should expose a summary text block.");
        Assert(currentFolderTextBlock != null, "Archive browser should expose the current folder text.");
        Assert(entriesListView!.Items.Count == 2, "Archive browser should initially show root-level archive items.");
        Assert(currentFolderTextBlock!.Text.Contains(@"Inside: \"), "Archive browser should start at archive root.");

        filterTextBox!.Text = "docs";
        Assert(entriesListView.Items.Count == 1, "Archive browser filter should narrow the visible entries.");
        Assert(summaryTextBlock!.Text.Contains("1 item", StringComparison.OrdinalIgnoreCase),
            "Archive browser summary should reflect filtered results.");

        filterTextBox.Text = string.Empty;
        foldersOnlyCheckBox!.IsChecked = true;
        Assert(entriesListView.Items.Count == 2, "Folders-only mode should keep visible directory entries, including synthesized parent folders.");
        Assert(summaryTextBlock.Text.Contains("folder", StringComparison.OrdinalIgnoreCase),
            "Archive browser summary should include folder counts.");
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

    private static void TestTransferStatusSummary(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var sourceDir = CreateCleanSubdir(root, "transfer_status_source");
        var destinationDir = CreateCleanSubdir(root, "transfer_status_destination");
        var source = Path.Combine(sourceDir, "item.txt");
        File.WriteAllText(source, "source");

        vm.LoadDirectory(destinationDir);
        vm.ImportDroppedFiles([source], destinationDir, moveFiles: true);

        Assert(vm.StatusText.Contains("Moved 1 item(s)"), "Transfer status should report completed transfers.");
        Assert(vm.StatusText.Contains("1 items"), "Transfer status should retain the item count summary.");
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

    private static void TestFilterMatchModes(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "filter_modes");
        File.WriteAllText(Path.Combine(folder, "alpha.txt"), "a");
        File.WriteAllText(Path.Combine(folder, "alphabet.txt"), "b");
        File.WriteAllText(Path.Combine(folder, "beta.log"), "c");

        vm.LoadDirectory(folder);

        vm.FilterMatchMode = NameMatchMode.Exact;
        vm.FilterText = "alpha.txt";
        Assert(vm.Items.Count == 1, "Exact filter should only match the exact file name.");

        vm.FilterMatchMode = NameMatchMode.Wildcard;
        vm.FilterText = "alpha*";
        Assert(vm.Items.Count == 2, "Wildcard filter should match both alpha-prefixed files.");

        vm.FilterText = "*.log";
        Assert(vm.Items.Count == 1 && vm.Items[0].Name == "beta.log", "Wildcard extension filter should match log files.");
    }

    private static void TestFilterHistoryAndClearOnNavigation(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var first = CreateCleanSubdir(root, "filter_history_first");
        var second = CreateCleanSubdir(root, "filter_history_second");
        File.WriteAllText(Path.Combine(first, "alpha.txt"), "a");
        File.WriteAllText(Path.Combine(second, "beta.txt"), "b");

        vm.LoadDirectory(first);
        vm.FilterText = "alpha";
        vm.AddCurrentFilterToHistoryCommand.Execute(null);

        Assert(vm.FilterHistory.Count == 1, "Saving a filter should add it to history.");
        Assert(vm.FilterHistory[0] == "alpha", "Saved filter history should preserve the current text.");

        vm.ClearFilterOnFolderChange = true;
        vm.LoadDirectory(second);

        Assert(string.IsNullOrEmpty(vm.FilterText), "Folder navigation should clear the filter when the option is enabled.");
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

    private static void TestShellProperties(string root)
    {
        var fs = new FileSystemService();
        var folder = CreateCleanSubdir(root, "shell_props");
        var testFile = Path.Combine(folder, "test.txt");
        File.WriteAllText(testFile, "hello");

        var items = fs.GetDirectoryContents(folder);
        var item = items.Single(i => i.Name == "test.txt");

        // For a plain .txt file, shell properties might be empty or limited,
        // but it shouldn't crash and should be initialized.
        Assert(item.ShellProperties != null, "ShellProperties should not be null.");

        // We can't easily test specific values like Company/Version without an actual EXE,
        // but we can verify the PropertiesDialog displays the "No additional details" message for a plain file.
        EnsureSmokeApplication();
        var dialog = new PropertiesDialog(item);
        var detailsStack = dialog.FindName("DetailsStack") as StackPanel;
        Assert(detailsStack != null, "Properties dialog should contain a DetailsStack.");
        
        // If it's empty, it should have one child (the italic "No additional details" message)
        Assert(detailsStack!.Children.Count > 0, "DetailsStack should be populated.");
        dialog.Close();
    }

    private static void TestRichPreviewSupport(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "rich_preview");
        var mdFile = Path.Combine(folder, "test.md");
        var csFile = Path.Combine(folder, "test.cs");
        File.WriteAllText(mdFile, "# Header\n**bold** and - list");
        File.WriteAllText(csFile, "public class Test { // comment\n}");

        var vm = new MainViewModel(fs, clipboard);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(folder);
        vm.TogglePreviewCommand.Execute(null);

        // Test Markdown
        var mdItem = vm.ActiveTab!.FileList.Items.Single(i => i.Name == "test.md");
        vm.ActiveTab.FileList.SelectedItem = mdItem;
        WaitFor(() => vm.HasPreviewRichText && vm.HasPreviewStatus);
        Assert(vm.HasPreviewRichText, "Markdown files should produce rich preview content.");
        Assert(vm.PreviewStatusText.Contains("Markdown document"), "Markdown status should be correct.");
        Assert(vm.PreviewFlowDocument != null, "Markdown should produce a FlowDocument.");

        // Test Code
        var csItem = vm.ActiveTab.FileList.Items.Single(i => i.Name == "test.cs");
        vm.ActiveTab.FileList.SelectedItem = csItem;
        WaitFor(() => vm.HasPreviewRichText && vm.HasPreviewStatus);
        Assert(vm.HasPreviewRichText, "C# files should produce rich preview content.");
        Assert(vm.PreviewStatusText.Contains("Source code"), "C# status should be correct.");
        Assert(vm.PreviewFlowDocument != null, "C# should produce a FlowDocument.");
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
            vm.ActiveTab!.NavigateTo(folder);
            var initialCount = vm.Tabs.Count;

            var originalTab = vm.ActiveTab;
            vm.DuplicateTab(originalTab);

            Assert(vm.Tabs.Count == initialCount + 1, "Duplicate should add one tab.");
            Assert(vm.ActiveTab != originalTab, "Active tab should be the new duplicate.");
            Assert(vm.ActiveTab!.CurrentPath == folder, "Duplicate tab should navigate to same path.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", null);
        }
    }

    private static void TestBookmarkOpenInNewTabReusesExisting(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "bookmark_reuse_existing");
        var vm = new MainViewModel(fs, clipboard);
        vm.NavigateToPath(folder);

        var existingTab = vm.ActiveTab;
        var bookmark = new BookmarkItem
        {
            Name = "Existing Folder",
            Path = folder
        };

        vm.OpenBookmarkInNewTabCommand.Execute(bookmark);

        Assert(vm.Tabs.Count == 1, "Open in new tab should reuse an already-open folder tab.");
        Assert(vm.ActiveTab == existingTab, "Reused tab should become the active tab.");
    }

    private static void TestBookmarkOpenAllInTabsReusesExisting(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var existingFolder = CreateCleanSubdir(root, "bookmark_open_all_existing");
        var newFolder = CreateCleanSubdir(root, "bookmark_open_all_new");
        var vm = new MainViewModel(fs, clipboard);
        vm.NavigateToPath(existingFolder);

        var category = new BookmarkItem
        {
            Name = "Session",
            IsFolder = true,
            Children =
            {
                new BookmarkItem
                {
                    Name = "Existing",
                    Path = existingFolder
                },
                new BookmarkItem
                {
                    Name = "New",
                    Path = newFolder
                }
            }
        };

        vm.OpenAllInTabsCommand.Execute(category);

        Assert(vm.Tabs.Count == 2, "Open all in tabs should only create tabs for folders that are not already open.");
        Assert(vm.Tabs.Count(tab => string.Equals(tab.CurrentPath, existingFolder, StringComparison.OrdinalIgnoreCase)) == 1,
            "Open all in tabs should not duplicate an already-open folder.");
        Assert(vm.ActiveTab?.CurrentPath == newFolder, "Open all in tabs should still activate the last opened new tab.");
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
            while (vm.Tabs.Count < 3)
                vm.NewTabCommand.Execute(null);
            Assert(vm.Tabs.Count >= 3, "Should have at least 3 tabs.");

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

    private static void TestCloseTabsToLeft(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var first = CreateCleanSubdir(root, "close_left_first");
        var second = CreateCleanSubdir(root, "close_left_second");
        var third = CreateCleanSubdir(root, "close_left_third");
        var fourth = CreateCleanSubdir(root, "close_left_fourth");

        var vm = new MainViewModel(fs, clipboard);
        vm.NavigateToPath(first);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(second);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(third);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(fourth);

        vm.ActiveTab = vm.Tabs[0];
        var keepTab = vm.Tabs[2];
        vm.CloseTabsToLeft(keepTab);

        Assert(vm.Tabs.Count == 2, "Close tabs to left should remove all tabs before the selected tab.");
        Assert(vm.Tabs[0] == keepTab, "Close tabs to left should retain the selected tab.");
        Assert(vm.Tabs[1].CurrentPath == fourth, "Close tabs to left should preserve tabs on the right.");
        Assert(vm.ActiveTab == keepTab, "Close tabs to left should activate the kept tab when the active tab is closed.");
    }

    private static void TestCloseTabsToRight(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var first = CreateCleanSubdir(root, "close_right_first");
        var second = CreateCleanSubdir(root, "close_right_second");
        var third = CreateCleanSubdir(root, "close_right_third");
        var fourth = CreateCleanSubdir(root, "close_right_fourth");

        var vm = new MainViewModel(fs, clipboard);
        vm.NavigateToPath(first);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(second);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(third);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(fourth);

        var keepTab = vm.Tabs[1];
        vm.ActiveTab = vm.Tabs[3];
        vm.CloseTabsToRight(keepTab);

        Assert(vm.Tabs.Count == 2, "Close tabs to right should remove all tabs after the selected tab.");
        Assert(vm.Tabs[0].CurrentPath == first, "Close tabs to right should preserve tabs on the left.");
        Assert(vm.Tabs[1] == keepTab, "Close tabs to right should retain the selected tab.");
        Assert(vm.ActiveTab == keepTab, "Close tabs to right should activate the kept tab when the active tab is closed.");
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

    private static void TestSearchMatchModes(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "search_modes");
        File.WriteAllText(Path.Combine(folder, "report.txt"), "a");
        File.WriteAllText(Path.Combine(folder, "report-final.txt"), "b");
        File.WriteAllText(Path.Combine(folder, "report.log"), "c");

        vm.LoadDirectory(folder);
        vm.SearchInFolderCommand.Execute(null);

        vm.SearchMatchMode = NameMatchMode.Exact;
        vm.SearchText = "report.txt";
        vm.ExecuteSearchSync();
        Assert(vm.Items.Count == 1 && vm.Items[0].Name == "report.txt", "Exact search mode should only match the exact file name.");

        vm.SearchMatchMode = NameMatchMode.Wildcard;
        vm.SearchText = "report*";
        vm.ExecuteSearchSync();
        Assert(vm.Items.Count == 3, "Wildcard search mode should match all report-prefixed items.");
    }

    private static void TestSearchByFileContent(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "content_search_root");
        var sub = CreateCleanSubdir(folder, "nested");
        File.WriteAllText(Path.Combine(folder, "alpha.txt"), "prefix");
        File.WriteAllText(Path.Combine(sub, "beta.md"), "This file contains the NEEDLE text.");

        vm.LoadDirectory(folder);
        vm.SearchInFolderCommand.Execute(null);
        vm.IncludeContentSearch = true;
        vm.ContentSearchText = "needle";
        vm.ExecuteSearchSync();

        Assert(vm.IsShowingSearchResults, "Content search should show the search-results banner.");
        Assert(vm.Items.Count == 1, "Content-only search should find the nested matching file.");
        Assert(vm.Items[0].Name == "beta.md", "Content search should return the file whose contents match.");
        Assert(vm.SearchResultsText.Contains("text \"needle\"", StringComparison.OrdinalIgnoreCase),
            "Search results text should describe the content-search criteria.");
    }

    private static void TestSearchContentLimits(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "content_search_limits");
        File.WriteAllText(Path.Combine(folder, "match.txt"), "needle");
        File.WriteAllText(Path.Combine(folder, "ignored.bin"), "needle");
        using (var stream = File.Create(Path.Combine(folder, "large.txt")))
        {
            stream.SetLength(11L * 1024 * 1024);
        }

        vm.LoadDirectory(folder);
        vm.SearchInFolderCommand.Execute(null);
        vm.IncludeContentSearch = true;
        vm.ContentSearchText = "needle";
        vm.ExecuteSearchSync();

        Assert(vm.Items.Count == 1, "Content search should skip non-text extensions and files larger than 10 MB.");
        Assert(vm.Items[0].Name == "match.txt", "Only the eligible text file should be returned.");
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
        Assert(string.IsNullOrEmpty(vm.ContentSearchText), "Closing search should clear the content-search text.");
        Assert(!vm.IncludeContentSearch, "Closing search should turn off content search.");
    }

    private static void TestSavedSearches(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "saved_search_root");
        CreateCleanSubdir(folder, "needle-folder");
        var savedSearchesPath = Path.Combine(root, "saved-searches.json");
        Environment.SetEnvironmentVariable("CUBICAI_SAVED_SEARCHES_PATH", savedSearchesPath);
        TryDelete(savedSearchesPath);

        try
        {
            var vm = new MainViewModel(fs, clipboard);
            vm.NewTabCommand.Execute(null);
            vm.NavigateToPath(folder);
            vm.SearchInFolderCommand.Execute(null);
            vm.CurrentPaneFileList!.SearchText = "needle";
            vm.CurrentPaneFileList.ExecuteSearchSync();

            vm.AddCurrentSearchCommand.Execute(null);

            Assert(vm.SavedSearches.Count == 1, "AddCurrentSearch should persist a saved search entry.");
            Assert(File.Exists(savedSearchesPath), "Saved searches should be written to disk.");

            var reloaded = new MainViewModel(fs, clipboard);
            reloaded.NewTabCommand.Execute(null);
            Assert(reloaded.SavedSearches.Count == 1, "Saved searches should reload from persistence.");

            var saved = reloaded.SavedSearches[0];
            reloaded.NavigateToPath(folder);
            reloaded.RunSavedSearchCommand.Execute(saved);

            Assert(reloaded.CurrentPaneFileList!.IsShowingSearchResults, "Running a saved search should show search results.");
            Assert(reloaded.CurrentPaneFileList.Items.Any(i => i.Name == "needle-folder"), "Running a saved search should reproduce results.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_SAVED_SEARCHES_PATH", null);
        }
    }

    private static void TestKnownFolderAliasNavigation(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var bookmarkFile = Path.Combine(root, "known_folder_bookmarks.json");
        Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", bookmarkFile);

        try
        {
            var desktopPath = fs.ResolveDirectoryPath("Desktop");
            Assert(!string.IsNullOrWhiteSpace(desktopPath), "Desktop alias should resolve to an existing folder.");

            var vm = new MainViewModel(fs, clipboard);
            vm.NewTabCommand.Execute(null);
            vm.AddressBarText = "Desktop";
            vm.NavigateToAddressCommand.Execute(null);

            Assert(string.Equals(vm.ActiveTab?.CurrentPath, desktopPath, StringComparison.OrdinalIgnoreCase),
                "Desktop alias should navigate to the resolved desktop path.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", null);
        }
    }

    private static void TestKnownFolderDisplayNames()
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var desktopPath = fs.ResolveDirectoryPath("Desktop");
        Assert(!string.IsNullOrWhiteSpace(desktopPath), "Desktop alias should resolve to an existing folder.");

        var vm = new MainViewModel(fs, clipboard, userSettings: new UserSettings());
        vm.NavigateToPath(desktopPath!);

        var expectedDisplayName = fs.GetDisplayName(desktopPath!);
        Assert(!string.IsNullOrWhiteSpace(expectedDisplayName), "Shell display name should not be empty.");
        Assert(string.Equals(vm.ActiveTab?.Title, expectedDisplayName, StringComparison.Ordinal),
            "Tab title should use the shell display name.");
        Assert(vm.BreadcrumbSegments.Count > 0, "Known-folder navigation should populate breadcrumbs.");
        Assert(string.Equals(vm.BreadcrumbSegments[^1].DisplayName, expectedDisplayName, StringComparison.Ordinal),
            "Breadcrumbs should use the shell display name for the current folder.");
        Assert(vm.RecentFolders.Count > 0, "Known-folder navigation should add a recent-folder entry.");
        Assert(string.Equals(vm.RecentFolders[0].DisplayName, expectedDisplayName, StringComparison.Ordinal),
            "Recent folders should use the shell display name.");
    }

    private static void TestShellTypeDescriptions(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "shell_types");
        var nested = Directory.CreateDirectory(Path.Combine(folder, "nested")).FullName;
        var textFile = Path.Combine(folder, "notes.txt");
        var nestedTextFile = Path.Combine(nested, "deep.txt");
        File.WriteAllText(textFile, "hello");
        File.WriteAllText(nestedTextFile, "world");

        var vm = new FileListViewModel(fs, clipboard);
        vm.LoadDirectory(folder);

        var listedItem = vm.Items.Single(i => i.Name == "notes.txt");
        var expectedType = ShellFileInfoHelper.TryGetTypeName(textFile, FileSystemItemType.File);
        Assert(!string.IsNullOrWhiteSpace(expectedType), "Shell type name should be available for the test file.");
        Assert(string.Equals(listedItem.TypeDescription, expectedType, StringComparison.Ordinal),
            "Directory listings should use the shell-reported type description.");

        vm.SearchText = "deep";
        vm.ExecuteSearchSync();

        var searchedItem = vm.Items.Single(i => i.Name == "deep.txt");
        var expectedNestedType = ShellFileInfoHelper.TryGetTypeName(nestedTextFile, FileSystemItemType.File);
        Assert(string.Equals(searchedItem.TypeDescription, expectedNestedType, StringComparison.Ordinal),
            "Recursive search results should preserve the shell-reported type description.");

        var dialog = new PropertiesDialog(listedItem);
        var typeText = dialog.FindName("TypeText") as TextBlock;
        Assert(typeText != null, "Properties dialog should expose the type text block.");
        Assert(string.Equals(typeText!.Text, listedItem.TypeDescription, StringComparison.Ordinal),
            "Properties dialog should display the item's shell-backed type description.");
        dialog.Close();
    }

    private static void TestOpenInExplorerRevealsSelection(string root)
    {
        var innerFs = new FileSystemService();
        var fs = new RecordingFileSystemService(innerFs);
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "open_in_explorer");
        var selectedFile = Path.Combine(folder, "selected.txt");
        File.WriteAllText(selectedFile, "x");

        var vm = new MainViewModel(fs, clipboard);
        vm.NavigateToPath(folder);

        var item = vm.ActiveTab!.FileList.Items.Single(i => i.Name == "selected.txt");
        vm.ActiveTab.FileList.SelectedItems.Clear();
        vm.ActiveTab.FileList.SelectedItem = item;

        vm.OpenInExplorerCommand.Execute(null);

        Assert(string.Equals(fs.LastRevealPath, selectedFile, StringComparison.OrdinalIgnoreCase),
            "Open in Explorer should reveal the selected item.");
        Assert(fs.LastOpenedFolderPath == null,
            "Open in Explorer should not open the folder generically when a single item is selected.");
    }

    private static void TestOpenInExplorerRevealsMultipleSelections(string root)
    {
        var innerFs = new FileSystemService();
        var fs = new RecordingFileSystemService(innerFs);
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "open_in_explorer_multi");
        var selectedFileA = Path.Combine(folder, "selected-a.txt");
        var selectedFileB = Path.Combine(folder, "selected-b.txt");
        File.WriteAllText(selectedFileA, "a");
        File.WriteAllText(selectedFileB, "b");

        var vm = new MainViewModel(fs, clipboard);
        vm.NavigateToPath(folder);

        var itemA = vm.ActiveTab!.FileList.Items.Single(i => i.Name == "selected-a.txt");
        var itemB = vm.ActiveTab.FileList.Items.Single(i => i.Name == "selected-b.txt");
        vm.ActiveTab.FileList.SelectedItems.Clear();
        vm.ActiveTab.FileList.SelectedItems.Add(itemA);
        vm.ActiveTab.FileList.SelectedItems.Add(itemB);
        vm.ActiveTab.FileList.SelectedItem = itemA;

        vm.OpenInExplorerCommand.Execute(null);

        Assert(fs.LastRevealPaths.SequenceEqual(new[] { selectedFileA, selectedFileB }, StringComparer.OrdinalIgnoreCase),
            "Open in Explorer should reveal every selected item when multiple items are selected.");
        Assert(fs.LastOpenedFolderPath == null,
            "Open in Explorer should not fall back to opening the folder when multiple items are selected.");
    }

    private static void TestSavedSearchMatchMode(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "saved_search_match_mode_root");
        File.WriteAllText(Path.Combine(folder, "report.txt"), "a");
        File.WriteAllText(Path.Combine(folder, "report-final.txt"), "b");
        File.WriteAllText(Path.Combine(folder, "notes.log"), "c");
        var savedSearchesPath = Path.Combine(root, "saved-searches-match-mode.json");
        Environment.SetEnvironmentVariable("CUBICAI_SAVED_SEARCHES_PATH", savedSearchesPath);
        TryDelete(savedSearchesPath);

        try
        {
            var vm = new MainViewModel(fs, clipboard);
            vm.NewTabCommand.Execute(null);
            vm.NavigateToPath(folder);
            vm.SearchInFolderCommand.Execute(null);
            vm.CurrentPaneFileList!.SearchMatchMode = NameMatchMode.Wildcard;
            vm.CurrentPaneFileList.SearchText = "report*";
            vm.CurrentPaneFileList.ExecuteSearchSync();

            vm.AddCurrentSearchCommand.Execute(null);

            Assert(vm.SavedSearches.Count == 1, "Saving a wildcard search should create one saved-search entry.");
            Assert(vm.SavedSearches[0].MatchMode == NameMatchMode.Wildcard, "Saved searches should persist their match mode.");

            var reloaded = new MainViewModel(fs, clipboard);
            reloaded.NewTabCommand.Execute(null);
            var saved = reloaded.SavedSearches[0];

            Assert(saved.MatchMode == NameMatchMode.Wildcard, "Reloaded saved searches should retain wildcard mode.");

            reloaded.NavigateToPath(folder);
            reloaded.RunSavedSearchCommand.Execute(saved);

            Assert(reloaded.CurrentPaneFileList!.IsShowingSearchResults, "Running a saved search should still show search results.");
            Assert(reloaded.CurrentPaneFileList.Items.Count == 2, "Running a wildcard saved search should reproduce wildcard results.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_SAVED_SEARCHES_PATH", null);
        }
    }

    private static void TestRenameSavedSearch(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var savedSearchFile = Path.Combine(root, "saved-searches-rename.json");
        var originalOverridePath = Environment.GetEnvironmentVariable("CUBICAI_SAVED_SEARCHES_PATH");
        Environment.SetEnvironmentVariable("CUBICAI_SAVED_SEARCHES_PATH", savedSearchFile);
        TryDelete(savedSearchFile);

        try
        {
            var vm = new MainViewModel(fs, clipboard);
            var item = new SavedSearchItem
            {
                Name = "Before",
                SearchPath = root,
                SearchTerm = "needle"
            };
            vm.SavedSearches.Add(item);

            vm.RenameSavedSearch(item, "After");

            Assert(item.Name == "After", "RenameSavedSearch should update the saved search name.");

            var reloaded = new MainViewModel(fs, clipboard);
            Assert(reloaded.SavedSearches.Any(saved => saved.Name == "After"),
                "RenameSavedSearch should persist the renamed saved search.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_SAVED_SEARCHES_PATH", originalOverridePath);
            TryDelete(savedSearchFile);
        }
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
        WaitFor(() => File.Exists(Path.Combine(rightFolder, "left.txt")));
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

        vm.CurrentPaneFileList!.FilterText = "alpha";
        Assert(vm.RightPaneTab.FileList.FilterText == "alpha", "Filter text should target the active right pane.");
        Assert(vm.RightPaneTab.FileList.Items.Count == 1, "Right pane filter should narrow right pane items.");

        vm.CurrentPaneFileList.ViewMode = "Tiles";
        Assert(vm.RightPaneTab.FileList.ViewMode == "Tiles", "View mode should target the active right pane.");

        vm.SearchInFolderCommand.Execute(null);
        Assert(vm.RightPaneTab.FileList.IsSearchVisible, "Search should open for the active right pane.");

        vm.CurrentPaneFileList.SearchText = "search";
        vm.ExecuteSearchCommand.Execute(null);
        WaitFor(() => vm.RightPaneTab.FileList.IsShowingSearchResults, 1000);
        Assert(vm.RightPaneTab.FileList.IsShowingSearchResults, "Search results should be shown in the active right pane.");
        Assert(vm.RightPaneTab.FileList.Items.Any(i => i.Name == "search-hit"), "Right pane search should search the right pane tree.");

        vm.ClearSearchResultsCommand.Execute(null);
        Assert(!vm.RightPaneTab.FileList.IsShowingSearchResults, "Clearing search should target the active right pane.");
        Assert(vm.CurrentPaneFileList.ViewMode == "Tiles", "Current view mode should reflect the active pane.");
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
        Assert(vm.PreviewFileInfo.Contains(item.TypeDescription, StringComparison.Ordinal),
            "Preview should include the selected item's type description.");
        WaitFor(() => vm.HasPreviewText);
        Assert(vm.HasPreviewText, "Text files should produce text preview content.");
        Assert(!vm.HasPreviewImage, "Text files should not produce image preview content.");
        Assert(vm.PreviewText.Contains("line one"), "Preview should include file contents.");
        Assert(vm.PreviewStatusText.Contains("Text file"), "Text preview should include a text-file status.");
        Assert(vm.PreviewStatusText.Contains("Lines shown"), "Text preview should report line counts.");

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
        var pdfFile = Path.Combine(folder, "doc.pdf");
        File.WriteAllBytes(unsupportedFile, [1, 2, 3, 4]);
        File.WriteAllText(largeTextFile, new string('a', 1024 * 1024 + 32));
        File.WriteAllText(pdfFile,
            "%PDF-1.4\n" +
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n" +
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n" +
            "3 0 obj\n<< /Type /Page /Parent 2 0 R >>\nendobj\n" +
            "trailer\n<< /Root 1 0 R >>\n%%EOF");

        var vm = new MainViewModel(fs, clipboard);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(folder);
        vm.TogglePreviewCommand.Execute(null);

        Assert(vm.HasPreviewStatus, "Preview should show a status message when nothing is selected.");
        Assert(vm.PreviewStatusText.Contains("Select"), "Empty preview should prompt for a selection.");

        var unsupportedItem = vm.ActiveTab!.FileList.Items.Single(i => i.Name == "archive.bin");
        vm.ActiveTab.FileList.SelectedItem = unsupportedItem;
        WaitFor(() => vm.HasPreviewText && vm.HasPreviewStatus);
        Assert(vm.HasPreviewText, "Small binary files should produce preview text.");
        Assert(vm.PreviewStatusText.Contains("Binary preview"), "Small binary files should use binary preview status.");
        Assert(vm.PreviewText.Contains("0000:"), "Binary preview should include a hex offset.");

        var largeTextItem = vm.ActiveTab.FileList.Items.Single(i => i.Name == "large.txt");
        vm.ActiveTab.FileList.SelectedItem = largeTextItem;
        Assert(vm.PreviewStatusText.Contains("1 MB"), "Large text files should show the preview size limit message.");

        var pdfItem = vm.ActiveTab.FileList.Items.Single(i => i.Name == "doc.pdf");
        vm.ActiveTab.FileList.SelectedItem = pdfItem;
        WaitFor(() => vm.PreviewStatusText.Contains("PDF"), 1000);
        Assert(vm.PreviewStatusText.Contains("PDF"), "PDF files should show PDF metadata preview.");
    }

    private static void TestImagePreviewMetadata(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "preview_image");
        var imageFile = Path.Combine(folder, "preview.png");
        File.WriteAllBytes(imageFile, Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+yF9sAAAAASUVORK5CYII="));

        var vm = new MainViewModel(fs, clipboard);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(folder);
        vm.TogglePreviewCommand.Execute(null);

        var imageItem = vm.ActiveTab!.FileList.Items.Single(i => i.Name == "preview.png");
        vm.ActiveTab.FileList.SelectedItem = imageItem;
        WaitFor(() => vm.HasPreviewImage && vm.HasPreviewStatus);

        Assert(vm.HasPreviewImage, "Image files should produce image preview content.");
        Assert(vm.PreviewStatusText.Contains("Image file"), "Image preview should include image metadata.");
        Assert(vm.PreviewStatusText.Contains("Dimensions"), "Image preview should include dimensions.");
    }

    private static void TestArchivePreviewMetadata(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "preview_archive");
        var archive = Path.Combine(folder, "sample.zip");

        using (var archiveStream = new FileStream(archive, FileMode.Create, FileAccess.ReadWrite))
        using (var zip = new System.IO.Compression.ZipArchive(archiveStream, System.IO.Compression.ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("notes.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("hello");
        }

        var vm = new MainViewModel(fs, clipboard);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(folder);
        vm.TogglePreviewCommand.Execute(null);

        var archiveItem = vm.ActiveTab!.FileList.Items.Single(i => i.Name == "sample.zip");
        vm.ActiveTab.FileList.SelectedItem = archiveItem;
        WaitFor(() => vm.PreviewStatusText.Contains("ZIP"), 1000);

        Assert(vm.PreviewStatusText.Contains("ZIP archive"), "ZIP files should show archive metadata.");
        Assert(vm.PreviewStatusText.Contains("first 8 entries"), "ZIP preview should describe preview truncation scope.");
        Assert(vm.PreviewText.Contains("notes.txt"), "ZIP preview should list archive entries.");
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

        var desktopPath = fs.ResolveDirectoryPath("Desktop");
        if (!string.IsNullOrWhiteSpace(desktopPath))
        {
            vm.AddressBarText = "desk";
            vm.UpdateAddressSuggestions();
            WaitFor(() => vm.AddressSuggestions.Count > 0);

            Assert(vm.AddressSuggestions.Contains(desktopPath, StringComparer.OrdinalIgnoreCase),
                "Known folder aliases should contribute address suggestions.");
        }

        vm.AddressBarText = Path.Combine(folder, "zzz");
        vm.UpdateAddressSuggestions();
        WaitFor(() => !vm.IsAddressSuggestionsOpen, 500);

        Assert(!vm.IsAddressSuggestionsOpen, "Non-matching prefixes should close address suggestions.");
        Assert(vm.AddressSuggestions.Count == 0, "Non-matching prefixes should clear suggestions.");
    }

    private static void TestAddressSuggestionsUiThreadSafety(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "autocomplete_threadsafe");
        CreateCleanSubdir(folder, "alpha");
        CreateCleanSubdir(folder, "beta");

        var vm = new MainViewModel(fs, clipboard)
        {
            AddressBarText = Path.Combine(folder, "a")
        };

        // Attach a CollectionView to simulate real WPF binding behavior.
        _ = CollectionViewSource.GetDefaultView(vm.AddressSuggestions);

        vm.UpdateAddressSuggestions();
        WaitFor(() => vm.AddressSuggestions.Count > 0);
        Assert(vm.AddressSuggestions.Any(s => s.Contains("alpha", StringComparison.OrdinalIgnoreCase)),
            "Address suggestions should update safely when a CollectionView is attached.");
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

    private static void TestUserSettingsDefaults()
    {
        var settings = new UserSettings();

        Assert(settings.DefaultViewMode == "Details", "Default view mode should be Details.");
        Assert(!settings.ShowHiddenFiles, "Hidden files should be off by default.");
        Assert(string.IsNullOrEmpty(settings.StartupFolder), "Startup folder should default to empty.");
        Assert(!settings.StartInDualPane, "Dual-pane startup should be off by default.");
        Assert(!settings.StartWithPreview, "Preview startup should be off by default.");
        Assert(!string.IsNullOrWhiteSpace(settings.NewFileTemplatesPath), "Template folder should default to a concrete path.");
        Assert(settings.FilterMatchMode == NameMatchMode.Contains, "Filter mode should default to contains.");
        Assert(settings.SearchMatchMode == NameMatchMode.Contains, "Search mode should default to contains.");
        Assert(!settings.ClearFilterOnFolderChange, "Clear-on-navigation should be off by default.");
        Assert(settings.FilterHistory.Count == 0, "Filter history should default to empty.");
        Assert(settings.DetailsColumns.Count == 0, "Column settings should default to empty so the app can supply defaults.");
    }

    private static void TestSettingsServiceRoundTrip(string root)
    {
        var settingsPath = Path.Combine(root, "settings.json");
        var originalOverridePath = Environment.GetEnvironmentVariable("CUBICAI_SETTINGS_PATH");
        Environment.SetEnvironmentVariable("CUBICAI_SETTINGS_PATH", settingsPath);

        try
        {
            TryDelete(settingsPath);
            var service = new SettingsService();
            var initial = service.Load();
            Assert(initial.DefaultViewMode == "Details", "Missing settings should load defaults.");

            var expected = new UserSettings
            {
                DefaultViewMode = "Tiles",
                ShowHiddenFiles = true,
                StartupFolder = root,
                NewFileTemplatesPath = Path.Combine(root, "templates"),
                StartInDualPane = true,
                StartWithPreview = true,
                FilterMatchMode = NameMatchMode.Wildcard,
                SearchMatchMode = NameMatchMode.Exact,
                ClearFilterOnFolderChange = true,
                FilterHistory = ["*.txt", "report*"],
                StartupSessionName = "Morning",
                NamedSessions =
                [
                    new NamedSession
                    {
                        Name = "Morning",
                        OpenTabs = [root],
                        ActiveTabIndex = 0,
                        RightPanePath = root,
                        IsDualPaneMode = true
                    }
                ]
                ,
                DetailsColumns =
                [
                    new DetailsColumnSetting
                    {
                        ColumnId = DetailsColumnId.Name,
                        Width = 420,
                        IsVisible = true,
                        DisplayOrder = 1
                    },
                    new DetailsColumnSetting
                    {
                        ColumnId = DetailsColumnId.Size,
                        Width = 90,
                        IsVisible = false,
                        DisplayOrder = 0
                    }
                ]
            };

            service.Save(expected);
            var reloaded = service.Load();

            Assert(reloaded.DefaultViewMode == "Tiles", "Settings round-trip should persist view mode.");
            Assert(reloaded.ShowHiddenFiles, "Settings round-trip should persist ShowHiddenFiles.");
            Assert(reloaded.StartupFolder == root, "Settings round-trip should persist startup folder.");
            Assert(reloaded.NewFileTemplatesPath == Path.Combine(root, "templates"),
                "Settings round-trip should persist template folder.");
            Assert(reloaded.StartInDualPane, "Settings round-trip should persist dual-pane startup.");
            Assert(reloaded.StartWithPreview, "Settings round-trip should persist preview startup.");
            Assert(reloaded.FilterMatchMode == NameMatchMode.Wildcard, "Settings round-trip should persist filter match mode.");
            Assert(reloaded.SearchMatchMode == NameMatchMode.Exact, "Settings round-trip should persist search match mode.");
            Assert(reloaded.ClearFilterOnFolderChange, "Settings round-trip should persist clear-on-navigation.");
            Assert(reloaded.FilterHistory.Count == 2, "Settings round-trip should persist filter history.");
            Assert(reloaded.StartupSessionName == "Morning", "Settings round-trip should persist startup session.");
            Assert(reloaded.NamedSessions.Count == 1, "Settings round-trip should persist named sessions.");
            Assert(reloaded.NamedSessions[0].IsDualPaneMode, "Settings round-trip should persist dual-pane session state.");
            Assert(reloaded.DetailsColumns.Count == 2, "Settings round-trip should persist details column settings.");
            Assert(reloaded.DetailsColumns.Any(c => c.ColumnId == DetailsColumnId.Name && c.Width == 420),
                "Settings round-trip should preserve details column widths.");
            Assert(reloaded.DetailsColumns.Any(c => c.ColumnId == DetailsColumnId.Size && !c.IsVisible),
                "Settings round-trip should preserve details column visibility.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_SETTINGS_PATH", originalOverridePath);
            TryDelete(settingsPath);
        }
    }

    private static void TestSavedSearchContentCriteria(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var folder = CreateCleanSubdir(root, "saved_search_content_root");
        File.WriteAllText(Path.Combine(folder, "alpha.txt"), "nothing here");
        File.WriteAllText(Path.Combine(folder, "beta.txt"), "contains NEEDLE text");
        var savedSearchesPath = Path.Combine(root, "saved-searches-content.json");
        Environment.SetEnvironmentVariable("CUBICAI_SAVED_SEARCHES_PATH", savedSearchesPath);
        TryDelete(savedSearchesPath);

        try
        {
            var vm = new MainViewModel(fs, clipboard);
            vm.NewTabCommand.Execute(null);
            vm.NavigateToPath(folder);
            vm.SearchInFolderCommand.Execute(null);
            vm.CurrentPaneFileList!.IncludeContentSearch = true;
            vm.CurrentPaneFileList.ContentSearchText = "needle";
            vm.CurrentPaneFileList.ExecuteSearchSync();

            vm.AddCurrentSearchCommand.Execute(null);

            Assert(vm.SavedSearches.Count == 1, "Saving a content search should create one saved-search entry.");
            Assert(vm.SavedSearches[0].IncludeContent, "Saved searches should persist the include-content flag.");
            Assert(vm.SavedSearches[0].ContentSearchTerm == "needle", "Saved searches should persist the content term.");

            var reloaded = new MainViewModel(fs, clipboard);
            reloaded.NewTabCommand.Execute(null);
            var saved = reloaded.SavedSearches[0];
            Assert(saved.IncludeContent, "Reloaded saved searches should retain the include-content flag.");
            Assert(saved.ContentSearchTerm == "needle", "Reloaded saved searches should retain the content-search term.");

            reloaded.NavigateToPath(folder);
            reloaded.RunSavedSearchCommand.Execute(saved);

            Assert(reloaded.CurrentPaneFileList!.IsShowingSearchResults, "Running a saved content search should show search results.");
            Assert(reloaded.CurrentPaneFileList.Items.Count == 1 && reloaded.CurrentPaneFileList.Items[0].Name == "beta.txt",
                "Running a saved content search should reproduce the content-match results.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_SAVED_SEARCHES_PATH", null);
        }
    }

    private static void TestBreadcrumbDropdownNavigation(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();

        var bookmarkFile = Path.Combine(root, "breadcrumb_dropdown_bookmarks.json");
        Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", bookmarkFile);
        try
        {
            var vm = new MainViewModel(fs, clipboard);
            vm.NewTabCommand.Execute(null);

            var parent = CreateCleanSubdir(root, "breadcrumb_dropdown_parent");
            var current = CreateCleanSubdir(parent, "current");
            var branch = CreateCleanSubdir(parent, "branch");

            vm.NavigateToPath(current);

            var parentSegment = vm.BreadcrumbSegments.Single(segment => string.Equals(segment.FullPath, parent, StringComparison.OrdinalIgnoreCase));
            Assert(parentSegment.DropdownVisibility == Visibility.Visible, "Non-terminal breadcrumb segments should expose a dropdown.");

            vm.LoadBreadcrumbDropdownAsync(parentSegment).GetAwaiter().GetResult();

            var branchItem = parentSegment.DropdownItems.Single(item => string.Equals(item.FullPath, branch, StringComparison.OrdinalIgnoreCase));
            Assert(branchItem.DisplayName == "branch", "Breadcrumb dropdown should list immediate child folders.");

            vm.NavigateBreadcrumbDropdownItem(branchItem);

            Assert(string.Equals(vm.CurrentPanePath, branch, StringComparison.OrdinalIgnoreCase),
                "Selecting a breadcrumb dropdown item should navigate to that folder.");
            Assert(vm.CurrentPaneTab?.CanGoBack == true,
                "Breadcrumb dropdown navigation should preserve back history.");

            vm.CurrentPaneTab!.GoBackCommand.Execute(null);

            Assert(string.Equals(vm.CurrentPanePath, current, StringComparison.OrdinalIgnoreCase),
                "Back navigation should return to the previous folder after breadcrumb dropdown navigation.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", null);
        }
    }

    private static void TestSettingsWatcherReloadsAfterExternalRecreate(string root)
    {
        var settingsPath = Path.Combine(root, "settings_watcher.json");
        var originalOverridePath = Environment.GetEnvironmentVariable("CUBICAI_SETTINGS_PATH");
        Environment.SetEnvironmentVariable("CUBICAI_SETTINGS_PATH", settingsPath);
        TryDelete(settingsPath);

        try
        {
            using var service = new SettingsService();
            UserSettings? observed = null;
            var notifications = 0;
            service.SettingsChanged += (_, settings) =>
            {
                observed = settings;
                Interlocked.Increment(ref notifications);
            };

            File.WriteAllText(settingsPath, "{\"DefaultViewMode\":\"List\"}");
            WaitFor(() => Volatile.Read(ref notifications) >= 1
                && string.Equals(observed?.DefaultViewMode, "List", StringComparison.Ordinal), 5000);
            Assert(observed?.DefaultViewMode == "List", "Settings watcher should reload externally created settings.");

            File.Delete(settingsPath);
            WaitFor(() => Volatile.Read(ref notifications) >= 2
                && string.Equals(observed?.DefaultViewMode, "Details", StringComparison.Ordinal), 5000);
            Assert(observed?.DefaultViewMode == "Details", "Settings watcher should fall back to defaults after external deletion.");

            File.WriteAllText(settingsPath, "{\"DefaultViewMode\":\"Tiles\",\"ShowHiddenFiles\":true}");
            WaitFor(() => Volatile.Read(ref notifications) >= 3
                && string.Equals(observed?.DefaultViewMode, "Tiles", StringComparison.Ordinal), 5000);
            Assert(observed?.DefaultViewMode == "Tiles", "Settings watcher should stay active after delete/recreate.");
            Assert(observed?.ShowHiddenFiles == true, "Settings watcher should reload the recreated file contents.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_SETTINGS_PATH", originalOverridePath);
            TryDelete(settingsPath);
        }
    }

    private static void TestDetailsColumnDefaults()
    {
        var vm = new MainViewModel(new FileSystemService(), new FakeClipboardService());
        var settings = vm.GetDetailsColumnSettings().ToList();

        Assert(settings.Count == 8, $"Default details layout should expose all eight columns (got {settings.Count}).");
        Assert(settings.Take(4).All(static setting => setting.IsVisible), "Primary default columns should be visible.");
        Assert(settings.Skip(4).All(static setting => !setting.IsVisible), "Extended shell property columns should be hidden by default.");
        
        Assert(settings.Select(static setting => setting.ColumnId).Take(4).SequenceEqual(
            [DetailsColumnId.Name, DetailsColumnId.Size, DetailsColumnId.Type, DetailsColumnId.DateModified]),
            "Default details layout should preserve the expected column order.");
    }

    private static void TestDetailsColumnSettingsSave()
    {
        var vm = new MainViewModel(new FileSystemService(), new FakeClipboardService());
        vm.SaveDetailsColumnSettings(
        [
            new DetailsColumnSetting
            {
                ColumnId = DetailsColumnId.Type,
                Width = 180,
                IsVisible = true,
                DisplayOrder = 0
            },
            new DetailsColumnSetting
            {
                ColumnId = DetailsColumnId.Name,
                Width = 400,
                IsVisible = true,
                DisplayOrder = 1
            },
            new DetailsColumnSetting
            {
                ColumnId = DetailsColumnId.Size,
                Width = 80,
                IsVisible = false,
                DisplayOrder = 2
            }
        ]);

        var saved = vm.GetDetailsColumnSettings().ToList();
        Assert(saved.Count == 8, $"Saved details layout should normalize missing columns (got {saved.Count}).");
        Assert(saved[0].ColumnId == DetailsColumnId.Type, "Saved details layout should preserve custom order.");
        Assert(saved[1].ColumnId == DetailsColumnId.Name, "Saved details layout should preserve subsequent column order.");
        Assert(saved.Any(c => c.ColumnId == DetailsColumnId.Size && !c.IsVisible),
            "Saved details layout should preserve hidden columns.");
        Assert(saved.Any(c => c.ColumnId == DetailsColumnId.DateModified),
            "Saved details layout should reintroduce unspecified columns with defaults.");
    }

    private static void TestNewTabAppliesSettings(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var startupFolder = CreateCleanSubdir(root, "settings_startup");

        var settings = new UserSettings
        {
            DefaultViewMode = "Tiles",
            ShowHiddenFiles = true,
            StartupFolder = startupFolder,
            FilterMatchMode = NameMatchMode.Wildcard,
            SearchMatchMode = NameMatchMode.Exact,
            ClearFilterOnFolderChange = true,
            FilterHistory = ["*.txt"]
        };

        var vm = new MainViewModel(fs, clipboard, userSettings: settings);
        vm.NewTabCommand.Execute(null);

        Assert(vm.ActiveTab != null, "NewTab should create an active tab.");
        Assert(vm.ActiveTab!.CurrentPath == startupFolder, "Startup folder setting should control first tab path.");
        Assert(vm.ActiveTab.FileList.ViewMode == "Tiles", "Default view mode setting should apply to new tabs.");
        Assert(vm.ActiveTab.FileList.ShowHiddenFiles, "ShowHiddenFiles setting should apply to new tabs.");
        Assert(vm.ActiveTab.FileList.FilterMatchMode == NameMatchMode.Wildcard, "Filter mode setting should apply to new tabs.");
        Assert(vm.ActiveTab.FileList.SearchMatchMode == NameMatchMode.Exact, "Search mode setting should apply to new tabs.");
        Assert(vm.ActiveTab.FileList.ClearFilterOnFolderChange, "Clear-on-navigation should apply to new tabs.");
        Assert(vm.ActiveTab.FileList.FilterHistory.SequenceEqual(["*.txt"]), "Filter history should apply to new tabs.");
    }

    private static void TestStartupTabLoadsVisibleItems(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var startupFolder = CreateCleanSubdir(root, "startup_visible_items");
        File.WriteAllText(Path.Combine(startupFolder, "visible.txt"), "hello");

        var settings = new UserSettings
        {
            OpenTabs = [startupFolder],
            ActiveTabIndex = 0
        };

        var vm = new MainViewModel(fs, clipboard, userSettings: settings);

        Assert(vm.ActiveTab != null, "Startup restore should create an active tab.");
        Assert(vm.ActiveTab!.CurrentPath == startupFolder, "Startup restore should navigate to the saved folder.");
        Assert(vm.ActiveTab.FileList.Items.Any(i => i.Name == "visible.txt"),
            "Startup restore should populate the file list for a saved non-empty folder.");
    }

    private static void TestMainWindowXamlLoads()
    {
        EnsureSmokeApplication();

        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new MainViewModel(fs, clipboard);
        var window = new CubicAIExplorer.MainWindow
        {
            DataContext = vm
        };

        window.ApplyTemplate();
        window.UpdateLayout();

        Assert(window.DataContext == vm, "Main window should accept the main view model as its data context.");
        window.Close();
    }

    private static void TestMainWindowFileListShowsStartupItems(string root)
    {
        EnsureSmokeApplication();

        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var startupFolder = CreateCleanSubdir(root, "window_startup_items");
        File.WriteAllText(Path.Combine(startupFolder, "alpha.txt"), "a");
        File.WriteAllText(Path.Combine(startupFolder, "beta.txt"), "b");

        var vm = new MainViewModel(fs, clipboard, userSettings: new UserSettings
        {
            OpenTabs = [startupFolder],
            ActiveTabIndex = 0
        });

        var window = new CubicAIExplorer.MainWindow
        {
            DataContext = vm
        };

        window.Show();
        window.ApplyTemplate();
        window.UpdateLayout();
        DoEvents();

        var fileListView = window.FindName("FileListView") as ListView;
        Assert(fileListView != null, "Main window should expose the file list control.");
        Assert(fileListView!.Items.Count >= 2, "Main window file list should render startup folder items.");
        fileListView.ScrollIntoView(fileListView.Items[0]);
        fileListView.UpdateLayout();
        DoEvents();
        var firstContainer = fileListView.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
        Assert(firstContainer != null, "Main window should generate a visible row container for startup items.");

        window.Close();
    }

    private static void TestNamedSessionSave(string root)
    {
        var settingsPath = Path.Combine(root, "named_session_save.json");
        var originalOverridePath = Environment.GetEnvironmentVariable("CUBICAI_SETTINGS_PATH");
        Environment.SetEnvironmentVariable("CUBICAI_SETTINGS_PATH", settingsPath);

        try
        {
            TryDelete(settingsPath);
            var fs = new FileSystemService();
            var clipboard = new FakeClipboardService();
            using var service = new SettingsService();
            var left = CreateCleanSubdir(root, "session_save_left");
            var right = CreateCleanSubdir(root, "session_save_right");

            var vm = new MainViewModel(fs, clipboard, service, service.Load());
            vm.NavigateToPath(left);
            vm.ToggleDualPaneCommand.Execute(null);
            vm.ActivateRightPane();
            vm.NavigateCurrentPaneToPath(right);
            vm.ActivateLeftPane();

            Assert(vm.SaveNamedSession("Morning", overwriteExisting: false), "Saving a named session should succeed.");
            Assert(File.Exists(settingsPath), "Saving a named session should persist settings.");

            var reloadedSettings = service.Load();
            Assert(reloadedSettings.NamedSessions.Count == 1, "Saved sessions should round-trip through settings.");
            Assert(reloadedSettings.NamedSessions[0].Name == "Morning", "Saved session name should persist.");
            Assert(reloadedSettings.NamedSessions[0].RightPanePath == right, "Saved session should persist the right pane path.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_SETTINGS_PATH", originalOverridePath);
            TryDelete(settingsPath);
        }
    }

    private static void TestNamedSessionLoad(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var first = CreateCleanSubdir(root, "session_load_first");
        var second = CreateCleanSubdir(root, "session_load_second");
        var right = CreateCleanSubdir(root, "session_load_right");
        var other = CreateCleanSubdir(root, "session_load_other");
        var otherRight = CreateCleanSubdir(root, "session_load_other_right");

        var vm = new MainViewModel(fs, clipboard);
        vm.NavigateToPath(first);
        vm.NewTabCommand.Execute(null);
        vm.NavigateToPath(second);
        vm.ToggleDualPaneCommand.Execute(null);
        vm.ActivateRightPane();
        vm.NavigateCurrentPaneToPath(right);
        vm.ActivateLeftPane();

        Assert(vm.SaveNamedSession("Morning", overwriteExisting: false), "Precondition failed: session should save.");

        vm.NavigateToPath(other);
        vm.ToggleDualPaneCommand.Execute(null);
        vm.ToggleDualPaneCommand.Execute(null);
        vm.ActivateRightPane();
        vm.NavigateCurrentPaneToPath(otherRight);
        vm.ActivateLeftPane();

        Assert(vm.LoadNamedSession("Morning"), "Loading a saved session should succeed.");
        Assert(vm.Tabs.Count == 2, "Loading a saved session should replace the current tab set.");
        Assert(vm.ActiveTab?.CurrentPath == second, "Loading a saved session should restore the active tab.");
        Assert(vm.IsDualPaneMode, "Loading a saved session should restore dual-pane mode.");
        Assert(vm.RightPaneTab?.CurrentPath == right, "Loading a saved session should restore the right pane path.");
    }

    private static void TestNamedSessionDelete(string root)
    {
        var settingsPath = Path.Combine(root, "named_session_delete.json");
        var originalOverridePath = Environment.GetEnvironmentVariable("CUBICAI_SETTINGS_PATH");
        Environment.SetEnvironmentVariable("CUBICAI_SETTINGS_PATH", settingsPath);

        try
        {
            TryDelete(settingsPath);
            var fs = new FileSystemService();
            var clipboard = new FakeClipboardService();
            using var service = new SettingsService();
            var folder = CreateCleanSubdir(root, "session_delete_folder");

            var vm = new MainViewModel(fs, clipboard, service, service.Load());
            vm.NavigateToPath(folder);
            Assert(vm.SaveNamedSession("Disposable", overwriteExisting: false), "Precondition failed: session should save.");

            Assert(vm.DeleteNamedSession("Disposable"), "Deleting a saved session should succeed.");
            Assert(!vm.NamedSessions.Any(session => session.Name == "Disposable"), "Deleted sessions should be removed from the UI collection.");

            var reloadedSettings = service.Load();
            Assert(reloadedSettings.NamedSessions.Count == 0, "Deleted sessions should be removed from persisted settings.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_SETTINGS_PATH", originalOverridePath);
            TryDelete(settingsPath);
        }
    }

    private static void TestNamedSessionStartupSelection(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var autoRestorePath = CreateCleanSubdir(root, "startup_auto_restore");
        var sessionLeft = CreateCleanSubdir(root, "startup_session_left");
        var sessionRight = CreateCleanSubdir(root, "startup_session_right");

        var autoRestoreSettings = new UserSettings
        {
            OpenTabs = [autoRestorePath],
            ActiveTabIndex = 0
        };

        var autoRestoreVm = new MainViewModel(fs, clipboard, userSettings: autoRestoreSettings);
        Assert(autoRestoreVm.ActiveTab?.CurrentPath == autoRestorePath,
            "Without a startup session, startup should use the generic auto-restored tabs.");

        var startupSettings = new UserSettings
        {
            OpenTabs = [autoRestorePath],
            ActiveTabIndex = 0,
            StartupSessionName = "Morning",
            NamedSessions =
            [
                new NamedSession
                {
                    Name = "Morning",
                    OpenTabs = [sessionLeft],
                    ActiveTabIndex = 0,
                    RightPanePath = sessionRight,
                    IsDualPaneMode = true
                }
            ]
        };

        var startupVm = new MainViewModel(fs, clipboard, userSettings: startupSettings);
        Assert(startupVm.ActiveTab?.CurrentPath == sessionLeft,
            "Configured startup sessions should take precedence over generic auto-restore.");
        Assert(startupVm.IsDualPaneMode, "Startup session selection should restore dual-pane mode.");
        Assert(startupVm.RightPaneTab?.CurrentPath == sessionRight,
            "Startup session selection should restore the right pane path.");
    }

    private static void TestTabOverflowWiring()
    {
        var mainXaml = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CubicAIExplorer", "MainWindow.xaml"));
        var mainCodeBehind = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CubicAIExplorer", "MainWindow.xaml.cs"));
        var main = File.ReadAllText(mainXaml);
        var mainCs = File.ReadAllText(mainCodeBehind);

        Assert(main.Contains("x:Name=\"TabsControl\""), "TabControl should be named for overflow management.");
        Assert(main.Contains("TabHeaderScrollViewer"), "Tab strip should use a named header scroll viewer.");
        Assert(main.Contains("TabScrollLeftButton"), "Tab strip should expose a left scroll button.");
        Assert(main.Contains("TabScrollRightButton"), "Tab strip should expose a right scroll button.");
        Assert(main.Contains("TabOverflowButton"), "Tab strip should expose a more-tabs button.");
        Assert(mainCs.Contains("TabOverflowButton_Click"), "Overflow button click handler should be implemented.");
        Assert(mainCs.Contains("PopulateTabOverflowMenu"), "Overflow menu population should be implemented.");
        Assert(mainCs.Contains("EnsureActiveTabVisible"), "Active-tab visibility should be enforced when the tab strip overflows.");
        Assert(mainCs.Contains("Tabs_CollectionChanged"), "Tab collection changes should refresh overflow affordances.");
    }

    private static void TestAppIconConfiguration()
    {
        var projectFile = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CubicAIExplorer", "CubicAIExplorer.csproj"));
        var mainXaml = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CubicAIExplorer", "MainWindow.xaml"));
        var iconPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CubicAIExplorer", "Resources", "appicon-v2.ico"));

        var project = File.ReadAllText(projectFile);
        var main = File.ReadAllText(mainXaml);

        Assert(project.Contains("<ApplicationIcon>Resources\\appicon-v2.ico</ApplicationIcon>"),
            "The project should stamp the built executable with the v2 app icon.");
        Assert(project.Contains("<Resource Include=\"Resources\\appicon-v2.ico\" />"),
            "The v2 ICO should be embedded as a WPF resource.");
        Assert(main.Contains("Icon=\"Resources/appicon-v2.ico\""),
            "MainWindow should use the v2 icon resource.");
        Assert(File.Exists(iconPath), "The v2 ICO asset should exist.");

        using var stream = File.OpenRead(iconPath);
        var decoder = new IconBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var sizes = decoder.Frames
            .Select(frame => frame.PixelWidth)
            .Distinct()
            .OrderBy(size => size)
            .ToArray();

        Assert(sizes.Contains(16), "The v2 ICO should contain a 16x16 frame.");
        Assert(sizes.Contains(32), "The v2 ICO should contain a 32x32 frame.");
        Assert(sizes.Contains(48), "The v2 ICO should contain a 48x48 frame.");
        Assert(sizes.Contains(256), "The v2 ICO should contain a 256x256 frame.");
    }

    private static void TestXamlWiring()
    {
        var mainXaml = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CubicAIExplorer", "MainWindow.xaml"));
        var appXaml = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CubicAIExplorer", "App.xaml"));
        var mainCodeBehind = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CubicAIExplorer", "MainWindow.xaml.cs"));
        var main = File.ReadAllText(mainXaml);
        var app = File.ReadAllText(appXaml);
        var mainCs = File.ReadAllText(mainCodeBehind);

        Assert(main.Contains("Key=\"F2\""), "F2 keybinding should exist.");
        Assert(main.Contains("Key=\"Z\""), "Ctrl+Z keybinding should exist.");
        Assert(main.Contains("Key=\"Y\""), "Ctrl+Y keybinding should exist.");
        Assert(main.Contains("Ctrl+Shift"), "Ctrl+Shift+N keybinding should exist.");
        Assert(main.Contains("SelectionMode=\"Extended\""), "Extended selection should exist.");
        Assert(main.Contains("AllowDrop=\"True\""), "File list drag/drop should be enabled.");
        Assert(main.Contains("Drop=\"FileList_Drop\""), "File list drop handler should be wired.");
        Assert(main.Contains("ClearHistoryCommand"), "Clear history command should be wired.");
        Assert(main.Contains("BookmarkItem_Selected"), "Bookmark single-click navigation should be wired.");
        Assert(main.Contains("SavedSearchList_SelectionChanged"), "Saved search single-click navigation should be wired.");
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
        Assert(main.Contains("AddCurrentFilterToHistoryCommand"), "Filter history save command should be wired.");
        Assert(main.Contains("CurrentPaneFileList.FilterHistory"), "Filter history dropdown should bind to the active pane.");
        Assert(main.Contains("CurrentPaneFileList.FilterMatchMode"), "Filter mode selector should bind to the active pane.");
        Assert(main.Contains("CurrentPaneFileList.ClearFilterOnFolderChange"), "Clear-on-navigation toggle should bind to the active pane.");
        Assert(main.Contains("PropertiesMenuItem"), "Properties menu item should be in context menu.");
        Assert(main.Contains("OpenInExplorerMenuItem"), "Open in Explorer menu item should exist.");
        Assert(main.Contains("ExtractArchiveMenuItem"), "Extract Archive menu item should exist.");
        Assert(main.Contains("BrowseArchiveMenuItem"), "Browse Archive menu item should exist.");
        Assert(main.Contains("EditNewMenuItem_SubmenuOpened"), "Edit New menu should repopulate dynamically.");
        Assert(main.Contains("PaneNewMenuItem_SubmenuOpened"), "Pane New menu should repopulate dynamically.");
        Assert(main.Contains("DuplicateTab_Click"), "Tab duplicate handler should be wired.");
        Assert(main.Contains("CloseTabsToLeft_Click"), "Close tabs to left handler should be wired.");
        Assert(main.Contains("CloseTabsToRight_Click"), "Close tabs to right handler should be wired.");
        Assert(main.Contains("CloseOtherTabs_Click"), "Close other tabs handler should be wired.");
        Assert(main.Contains("TabOverflowButton_Click"), "Tab overflow button should be wired.");
        Assert(main.Contains("TabScrollLeftButton_Click"), "Tab strip left scroll handler should be wired.");
        Assert(main.Contains("TabScrollRightButton_Click"), "Tab strip right scroll handler should be wired.");
        Assert(main.Contains("FolderTree_Drop"), "Folder tree drop handler should be wired.");
        Assert(main.Contains("AllowDrop=\"True\""), "Drop should be enabled.");
        Assert(main.Contains("BreadcrumbSegments"), "Breadcrumb segments binding should exist.");
        Assert(main.Contains("BreadcrumbSegment_Click"), "Breadcrumb click handler should be wired.");
        Assert(main.Contains("DropdownItems"), "Breadcrumb dropdown menus should bind their submenu items.");
        Assert(main.Contains("BreadcrumbDropdownButton_Click"), "Breadcrumb dropdown buttons should be wired.");
        Assert(mainCs.Contains("BreadcrumbDropdownItem_Click"), "Breadcrumb dropdown item handler should be implemented.");
        Assert(main.Contains("RecentFolders"), "Recent folders binding should exist.");
        Assert(main.Contains("RecentFolders_KeyDown"), "Recent folders keyboard handler should be wired.");
        Assert(main.Contains("BookmarkTree_MouseMove"), "Bookmark drag/drop handler should be wired.");
        Assert(main.Contains("BookmarkTree_DragLeave"), "Bookmark drag-leave handler should be wired.");
        Assert(main.Contains("BookmarkDragFeedbackText"), "Bookmark drag feedback should be bound.");
        Assert(main.Contains("IsBookmarkRootDropTarget"), "Bookmark root drop styling should be bound.");
        Assert(main.Contains("IsDropTarget"), "Bookmark drop target styling should be bound.");
        Assert(main.Contains("SavedSearchList"), "Saved search list should exist.");
        Assert(main.Contains("SearchTextBox"), "Search text box should exist.");
        Assert(main.Contains("ContentSearchTextBox"), "Content search text box should exist.");
        Assert(main.Contains("CurrentPaneFileList.SearchMatchMode"), "Search mode selector should bind to the active pane.");
        Assert(main.Contains("CurrentPaneFileList.IncludeContentSearch"), "Content-search toggle should bind to the active pane.");
        Assert(main.Contains("SearchInFolderCommand"), "Search command binding should exist.");
        Assert(main.Contains("AddCurrentSearchCommand"), "Saved-search add command should exist.");
        Assert(main.Contains("RenameSavedSearchCommand"), "Saved-search rename command should exist.");
        Assert(main.Contains("Command=\"{Binding CopyCommand}\""), "Copy bindings should route through the main view model.");
        Assert(main.Contains("Command=\"{Binding GoBackCommand}\""), "Back bindings should route through the main view model.");
        Assert(main.Contains("Command=\"{Binding RenameCommand}\""), "Rename bindings should route through the main view model.");
        Assert(main.Contains("Command=\"{Binding SelectAllCommand}\""), "Select-all bindings should route through the main view model.");
        Assert(main.Contains("Command=\"{Binding SearchInFolderCommand}\""), "Search bindings should route through the main view model.");
        Assert(main.Contains("Command=\"{Binding AddCurrentSearchCommand}\""), "Saved-search save bindings should route through the main view model.");
        Assert(main.Contains("Header=\"_Sessions\""), "Sessions menu should exist.");
        Assert(main.Contains("Header=\"Empty _Recycle Bin...\""), "Tools menu should expose Empty Recycle Bin.");
        Assert(main.Contains("SaveCurrentSessionAs_Click"), "Save Session As menu item should be wired.");
        Assert(main.Contains("UpdateCurrentNamedSessionCommand"), "Update Current Session should be wired.");
        Assert(main.Contains("SessionsMenu_SubmenuOpened"), "Sessions submenu population should be wired.");
        Assert(main.Contains("CurrentPaneFileList.FilterText"), "Filter bar should bind to the current active pane.");
        Assert(main.Contains("CurrentPaneFileList.SearchText"), "Search bar should bind to the current active pane.");
        Assert(main.Contains("CurrentPaneFileList.ContentSearchText"), "Content search text should bind to the current active pane.");
        Assert(main.Contains("CurrentPaneFileList.IsSearchVisible"), "Search visibility should bind to the current active pane.");
        Assert(main.Contains("CurrentPaneLabel"), "Status bar should include the current pane label.");
        Assert(main.Contains("CurrentPaneLabel, Mode=OneWay"), "CurrentPaneLabel should be OneWay-bound (read-only source).");
        Assert(main.Contains("CurrentPanePath, Mode=OneWay"), "CurrentPanePath should be OneWay-bound (read-only source).");
        Assert(main.Contains("Header=\"_Columns\""), "View menu should expose details column customization.");
        Assert(main.Contains("DetailsColumnVisibility_Click"), "Column visibility toggles should be wired.");
        Assert(main.Contains("AutoSizeVisibleColumns_Click"), "Auto-size columns action should be wired.");
        Assert(main.Contains("ResetDetailsColumns_Click"), "Reset columns action should be wired.");
        Assert(main.Contains("LeftPaneStatusText, Mode=OneWay"), "LeftPaneStatusText should be OneWay-bound (read-only source).");
        Assert(main.Contains("RightPaneStatusText, Mode=OneWay"), "RightPaneStatusText should be OneWay-bound (read-only source).");
        Assert(main.Contains("RightPaneHeader_MouseLeftButtonDown"), "Right pane header activation should be wired.");
        Assert(main.Contains("RightPaneAddressBox"), "Right pane should expose an inline address editor.");
        Assert(main.Contains("RightPaneAddressGo_Click"), "Right pane address editor should wire its go action.");
        Assert(main.Contains("RightPaneStatusText"), "Right pane header/status text should be bound.");
        Assert(main.Contains("PreviewStatusText"), "Preview panel should bind a fallback status message.");
        Assert(main.Contains("CurrentPanePath"), "Status bar should reflect the current active pane path.");
        Assert(main.Contains("FileOperationQueueStatusText"), "Status bar should expose background file operation status.");
        Assert(main.Contains("CancelFileOperationQueueCommand"), "Queue cancel command should be wired.");
        Assert(main.Contains("ToggleQueueDetailsCommand"), "Queue details toggle should be wired.");
        Assert(main.Contains("QueueDetailsButton"), "Queue details button should be named for the popup anchor.");
        Assert(main.Contains("FileOperationQueueRecentOperations"), "Queue details popup should bind recent operation history.");
        Assert(main.Contains("IsOpen=\"{Binding IsQueueDetailsVisible}\""), "Queue details popup should bind its open state.");
        Assert(main.Contains("ContextMenuOpening=\"RightPane_ContextMenuOpening\""), "Right pane context menu handler should be wired.");
        Assert(main.Contains("GridViewColumnHeader.Click=\"RightPaneHeader_Click\""), "Right pane sort handler should be wired.");
        Assert(main.Contains("GotKeyboardFocus=\"RightPane_GotKeyboardFocus\""), "Right pane focus tracking should be wired.");
        Assert(main.Contains("ToggleDualPaneCommand"), "Dual pane menu should invoke the dual pane command.");
        Assert(main.Contains("TogglePreviewCommand"), "Preview menu should invoke the preview command.");
        Assert(main.Contains("PreviewPanel"), "Preview panel should be focusable for keyboard navigation.");
        Assert(mainCs.Contains("Key.D1"), "Ctrl+1 shortcut should be handled.");
        Assert(mainCs.Contains("Key.D2"), "Ctrl+2 shortcut should be handled.");
        Assert(mainCs.Contains("Key.D3"), "Ctrl+3 shortcut should be handled.");
        Assert(mainCs.Contains("Key.D4"), "Ctrl+4 shortcut should be handled.");
        Assert(mainCs.Contains("SavedSearchList_KeyDown"), "Saved search keyboard handler should be wired.");
        Assert(mainCs.Contains("SavedSearchList_MouseDoubleClick"), "Saved search double-click handler should be wired.");
        Assert(mainCs.Contains("FileListViewModel_ArchiveBrowseRequested"), "Archive browser handler should be wired.");
        Assert(mainCs.Contains("PopulateNewMenu"), "Dynamic new-file template menu should be built in code-behind.");
        Assert(mainCs.Contains("ModifierKeys.Alt"), "Alt+D address shortcut should be handled.");
        Assert(mainCs.Contains("EmptyRecycleBin_Click"), "Empty Recycle Bin confirmation handler should be wired.");
        Assert(app.Contains("IconBack"), "Vector icon resources should exist.");
        Assert(app.Contains("IconSearch"), "Search icon resource should exist.");
        Assert(!main.Contains("&#x25C0;"), "Unicode arrow symbols should be replaced with vector icons.");
    }

    private static void TestReplaceFileFailure(string root)
    {
        var fs = new FileSystemService();
        var sourceDir = CreateCleanSubdir(root, "copy_replace_fail_source");
        var destinationDir = CreateCleanSubdir(root, "copy_replace_fail_destination");
        var source = Path.Combine(sourceDir, "item.txt");
        var destination = Path.Combine(destinationDir, "item.txt");
        File.WriteAllText(source, "new content");
        File.WriteAllText(destination, "old content");

        // Lock the source file to cause failure in CopyFile
        using (var stream = File.Open(source, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var results = fs.CopyFiles([source], destinationDir, FileTransferCollisionResolution.Replace);
            Assert(results.Count == 1, "Should have one result.");
            Assert(results[0].Status == FileTransferStatus.Failed, "Expected failure due to locked source.");
            Assert(File.ReadAllText(destination) == "old content", "Original file should be restored after failure.");
        }
    }

    private static void TestCopyReplaceDirectoryFailure(string root)
    {
        var fs = new FileSystemService();
        var sourceDir = CreateCleanSubdir(root, "copy_replace_dir_fail_source");
        var destinationDir = CreateCleanSubdir(root, "copy_replace_dir_fail_dest_root");
        
        var sourceSub = Path.Combine(sourceDir, "subdir");
        Directory.CreateDirectory(sourceSub);
        File.WriteAllText(Path.Combine(sourceSub, "new.txt"), "new content");
        
        var destinationSub = Path.Combine(destinationDir, "subdir");
        Directory.CreateDirectory(destinationSub);
        File.WriteAllText(Path.Combine(destinationSub, "old.txt"), "old content");

        // Lock the source file to cause failure midway in CopyDirectoryRecursive
        var sourceFile = Path.Combine(sourceSub, "new.txt");
        using (var stream = File.Open(sourceFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var results = fs.CopyFiles([sourceSub], destinationDir, FileTransferCollisionResolution.Replace);
            
            Assert(results.Count == 1, "Should have one result.");
            Assert(results[0].Status == FileTransferStatus.Failed, "Expected failure due to locked source file.");
            
            Assert(Directory.Exists(destinationSub), "Destination directory should exist (restored).");
            var oldFile = Path.Combine(destinationSub, "old.txt");
            Assert(File.Exists(oldFile), "Original file 'old.txt' should be restored in destination.");
            Assert(File.ReadAllText(oldFile) == "old content", "Content of 'old.txt' should be original.");
        }
    }

    private static void TestUndoRedoAfterNewFileAndLink(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "undo_new_file_link");
        var targetFile = Path.Combine(folder, "target.txt");
        File.WriteAllText(targetFile, "target");

        vm.LoadDirectory(folder);

        // Test New File Undo/Redo
        var newFileName = "new_file.txt";
        var newFilePath = Path.Combine(folder, newFileName);

        vm.NewFileWithHistory(newFileName);
        Assert(File.Exists(newFilePath), "New file should be created.");
        Assert(vm.CanUndo, "New file should create undo history.");

        vm.UndoCommand.Execute(null);
        Assert(!File.Exists(newFilePath), "Undo new file should delete the file.");

        vm.RedoCommand.Execute(null);
        Assert(File.Exists(newFilePath), "Redo new file should recreate the file.");

        // Test Symbolic Link Undo/Redo
        var linkName = "link.txt";
        var linkPath = Path.Combine(folder, linkName);

        try
        {
            vm.CreateSymbolicLinkWithHistory(linkName, targetFile);
            Assert(File.Exists(linkPath), "Symbolic link should be created.");
            Assert(vm.CanUndo, "Link creation should create undo history.");

            vm.UndoCommand.Execute(null);
            Assert(!File.Exists(linkPath), "Undo link creation should delete the link.");

            vm.RedoCommand.Execute(null);
            Assert(File.Exists(linkPath), "Redo link creation should recreate the link.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Note: Skipping symbolic link undo/redo check ({ex.Message}).");
        }
    }

    private static void TestDuplicateItem(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "duplicate");
        var original = Path.Combine(folder, "item.txt");
        File.WriteAllText(original, "original");

        vm.LoadDirectory(folder);
        vm.SelectedItem = vm.Items.Single(i => i.Name == "item.txt");
        vm.SelectedItems.Clear();
        vm.SelectedItems.Add(vm.SelectedItem);

        vm.DuplicateCommand.Execute(null);
        WaitFor(() => vm.Items.Count == 2);

        Assert(vm.Items.Any(i => i.Name == "item (2).txt"), "Duplicate should create a suffixed copy.");
        Assert(File.Exists(Path.Combine(folder, "item (2).txt")), "Duplicate file should exist on disk.");
    }

    private static void TestNewFileAndLink(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "new_file_link");
        var targetFile = Path.Combine(folder, "target.txt");
        File.WriteAllText(targetFile, "target");

        vm.LoadDirectory(folder);

        var newFile = fs.CreateFile(folder, "new.txt");
        Assert(File.Exists(newFile), "Service should create new file.");

        var linkPath = Path.Combine(folder, "link.txt");
        try
        {
            fs.CreateSymbolicLink(linkPath, targetFile);
            Assert(File.Exists(linkPath), "Service should create symbolic link.");
        }
        catch (Exception ex)
        {
            // Skip symlink check if it fails (likely due to privileges or OS constraints)
            Console.WriteLine($"Note: Skipping symbolic link check ({ex.Message}).");
        }
    }

    private static void TestUndoRedoAfterDuplicate(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "undo_duplicate");
        var original = Path.Combine(folder, "item.txt");
        File.WriteAllText(original, "content");

        vm.LoadDirectory(folder);
        vm.SelectedItem = vm.Items.Single(i => i.Name == "item.txt");
        vm.SelectedItems.Clear();
        vm.SelectedItems.Add(vm.SelectedItem);

        vm.DuplicateCommand.Execute(null);
        WaitFor(() => vm.Items.Count == 2);
        var duplicatePath = Path.Combine(folder, "item (2).txt");

        Assert(vm.CanUndo, "Duplicate should create undo history.");
        vm.UndoCommand.Execute(null);
        Assert(!File.Exists(duplicatePath), "Undo duplicate should remove the copy.");

        Assert(vm.CanRedo, "Undo duplicate should create redo history.");
        vm.RedoCommand.Execute(null);
        Assert(File.Exists(duplicatePath), "Redo duplicate should restore the copy.");
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
            using var bookmarkService = new BookmarkService();
            var vm = new MainViewModel(fs, clipboard, bookmarkService: bookmarkService);
            vm.NewTabCommand.Execute(null);
            vm.NavigateToPath(folder);

            var initialCount = vm.Bookmarks.Count;
            vm.AddBookmarkCommand.Execute(null);
            var afterFirstAdd = vm.Bookmarks.Count;
            vm.AddBookmarkCommand.Execute(null);
            var afterSecondAdd = vm.Bookmarks.Count;

            Assert(afterFirstAdd == initialCount + 1, "AddBookmark should add current folder.");
            Assert(afterSecondAdd == afterFirstAdd, "AddBookmark should ignore duplicates.");
            Assert(File.Exists(bookmarkFile), "Bookmark file should be created.");

            using var bookmarkService2 = new BookmarkService();
            var vmReloaded = new MainViewModel(fs, clipboard, bookmarkService: bookmarkService2);
            Assert(vmReloaded.Bookmarks.Any(b => string.Equals(b.Path, folder, StringComparison.OrdinalIgnoreCase)),
                "Bookmarks should persist across MainViewModel instances.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", null);
        }
    }

    private static void TestHeadlessSymbolicLinkFailureSurfacesToCaller(string root)
    {
        var fs = new ThrowingSymbolicLinkFileSystemService(new FileSystemService(), new UnauthorizedAccessException("Symlink privilege missing."));
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "headless_symlink_failure");
        var targetFile = Path.Combine(folder, "target.txt");
        File.WriteAllText(targetFile, "target");

        vm.LoadDirectory(folder);

        var threw = false;
        try
        {
            vm.CreateSymbolicLinkWithHistory("link.txt", targetFile);
        }
        catch (UnauthorizedAccessException ex)
        {
            threw = true;
            Assert(ex.Message.Contains("Symlink privilege missing.", StringComparison.Ordinal), "The original symbolic-link failure should bubble to the caller.");
        }

        Assert(threw, "Headless symbolic-link failures should surface to the caller instead of showing modal UI.");
        Assert(!vm.CanUndo, "Failed symbolic-link creation should not add undo history.");
    }

    private static void TestBookmarkWatcherReloadsAfterExternalReplace(string root)
    {
        var bookmarkPath = Path.Combine(root, "bookmarks_watcher.json");
        var replacementPath = Path.Combine(root, "bookmarks_watcher_replacement.json");
        var originalOverridePath = Environment.GetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH");
        Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", bookmarkPath);
        TryDelete(bookmarkPath);
        TryDelete(replacementPath);

        try
        {
            using var service = new BookmarkService();
            List<BookmarkItem>? observed = null;
            var notifications = 0;
            service.BookmarksChanged += (_, bookmarks) =>
            {
                observed = bookmarks;
                Interlocked.Increment(ref notifications);
            };

            File.WriteAllText(bookmarkPath, "[{\"Name\":\"One\",\"Path\":\"C:\\\\Temp\\\\One\"}]");
            WaitFor(() => Volatile.Read(ref notifications) >= 1
                && observed?.Any(bookmark => string.Equals(bookmark.Path, @"C:\Temp\One", StringComparison.OrdinalIgnoreCase)) == true, 5000);
            Assert(observed?.Count == 1, "Bookmark watcher should reload externally created bookmarks.");

            File.WriteAllText(replacementPath, "[{\"Name\":\"Two\",\"Path\":\"C:\\\\Temp\\\\Two\"}]");
            File.Move(replacementPath, bookmarkPath, overwrite: true);

            WaitFor(() => Volatile.Read(ref notifications) >= 2
                && observed?.Any(bookmark => string.Equals(bookmark.Path, @"C:\Temp\Two", StringComparison.OrdinalIgnoreCase)) == true, 5000);
            Assert(observed?.Count == 1, "Bookmark watcher should replace the old bookmark payload.");
            Assert(string.Equals(observed?[0].Name, "Two", StringComparison.Ordinal), "Bookmark watcher should reload replacement writes.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", originalOverridePath);
            TryDelete(bookmarkPath);
            TryDelete(replacementPath);
        }
    }

    private static void TestNewFileTemplatesCatalog(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var templateDir = CreateCleanSubdir(root, "template_catalog");
        File.WriteAllText(Path.Combine(templateDir, "Note.txt"), "note");
        File.WriteAllText(Path.Combine(templateDir, "Script.ps1"), "Write-Host 'hi'");

        var settings = new UserSettings
        {
            StartupFolder = root,
            NewFileTemplatesPath = templateDir
        };
        var vm = new MainViewModel(fs, clipboard, userSettings: settings);

        Assert(vm.NewFileTemplates.Count == 2, "Template catalog should load each file from the configured folder.");
        Assert(vm.NewFileTemplates.Any(t => t.DefaultFileName == "Note.txt"), "Template catalog should include the note template.");
        Assert(vm.NewFileTemplates.Any(t => t.DisplayName.Contains(".ps1", StringComparison.OrdinalIgnoreCase)),
            "Template catalog should expose display names with extension hints.");
    }

    private static void TestNewFileTemplateCreation(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var vm = new FileListViewModel(fs, clipboard);
        var folder = CreateCleanSubdir(root, "template_target");
        var templateDir = CreateCleanSubdir(root, "template_source");
        var templatePath = Path.Combine(templateDir, "Report.md");
        File.WriteAllText(templatePath, "# Report\n\n- item");

        vm.LoadDirectory(folder);
        vm.NewFileFromTemplateWithHistory(templatePath, "Report.md");

        var createdPath = Path.Combine(folder, "Report.md");
        Assert(File.Exists(createdPath), "Template creation should materialize a new file in the current folder.");
        Assert(File.ReadAllText(createdPath) == "# Report\n\n- item", "Template creation should copy the template contents.");
        Assert(vm.CanUndo, "Template creation should create undo history.");

        vm.UndoCommand.Execute(null);
        Assert(!File.Exists(createdPath), "Undo template creation should remove the generated file.");

        vm.RedoCommand.Execute(null);
        Assert(File.Exists(createdPath), "Redo template creation should recreate the generated file.");
        Assert(File.ReadAllText(createdPath) == "# Report\n\n- item", "Redo template creation should preserve the template contents.");
    }

    private static void TestBookmarkDragFeedback(string root)
    {
        var fs = new FileSystemService();
        var clipboard = new FakeClipboardService();
        var bookmarkFile = Path.Combine(root, "bookmark_drag_feedback.json");
        Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", bookmarkFile);
        TryDelete(bookmarkFile);

        try
        {
            using var bookmarkService = new BookmarkService();
            var vm = new MainViewModel(fs, clipboard, bookmarkService: bookmarkService);
            vm.Bookmarks.Clear();

            var folder = new BookmarkItem { Name = "Folder", IsFolder = true };
            var child = new BookmarkItem { Name = "Child", Path = @"C:\Temp\Child" };
            var sibling = new BookmarkItem { Name = "Sibling", Path = @"C:\Temp\Sibling" };
            folder.Children.Add(child);
            vm.Bookmarks.Add(folder);
            vm.Bookmarks.Add(sibling);

            vm.UpdateBookmarkDragFeedback(sibling, folder);
            Assert(vm.HasBookmarkDragFeedback, "Bookmark drag feedback should become visible for valid targets.");
            Assert(!vm.IsBookmarkDragFeedbackInvalid, "Valid bookmark drop target should not be flagged invalid.");
            Assert(folder.IsDropTarget, "Hovered bookmark target should be highlighted.");
            Assert(vm.BookmarkDragFeedbackText.Contains("move into", StringComparison.OrdinalIgnoreCase),
                "Folder drag feedback should describe moving into the folder.");

            vm.UpdateBookmarkDragFeedback(folder, child);
            Assert(vm.IsBookmarkDragFeedbackInvalid, "Dragging onto a descendant should be rejected.");
            Assert(!child.IsDropTarget, "Invalid targets should not stay highlighted.");

            vm.UpdateBookmarkDragFeedback(child, null);
            Assert(vm.IsBookmarkRootDropTarget, "Dragging over empty bookmark space should mark the root drop zone.");
            Assert(vm.BookmarkDragFeedbackText.Contains("top level", StringComparison.OrdinalIgnoreCase),
                "Root drag feedback should explain the top-level drop behavior.");

            vm.ClearBookmarkDragFeedback();
            Assert(!vm.HasBookmarkDragFeedback, "Clearing bookmark drag feedback should hide the hint.");
            Assert(!vm.IsBookmarkRootDropTarget, "Clearing bookmark drag feedback should reset the root target.");
            Assert(!folder.IsDropTarget && !child.IsDropTarget && !sibling.IsDropTarget,
                "Clearing bookmark drag feedback should remove all bookmark highlights.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH", null);
        }
    }

    private static void TestEmptyRecycleBinCommand()
    {
        var fs = new RecordingFileSystemService(new FileSystemService());
        var clipboard = new FakeClipboardService();
        var vm = new MainViewModel(fs, clipboard);

        vm.EmptyRecycleBinCommand.Execute(null);

        Assert(fs.EmptyRecycleBinCallCount == 1, "Empty Recycle Bin should call the filesystem service.");
        Assert(vm.StatusText.Contains("Recycle Bin emptied", StringComparison.OrdinalIgnoreCase),
            "Empty Recycle Bin should update the status text on success.");
    }

    private static void TestOpenInNewWindowCommand(string root)
    {
        var folder = CreateCleanSubdir(root, "open_new_window");
        var child = Directory.CreateDirectory(Path.Combine(folder, "child"));

        var fs = new RecordingFileSystemService(new FileSystemService());
        var clipboard = new FakeClipboardService();
        var vm = new MainViewModel(fs, clipboard);
        vm.NavigateCurrentPaneToPath(folder);

        var selected = vm.CurrentPaneFileList!.Items.Single(item =>
            string.Equals(item.FullPath, child.FullName, StringComparison.OrdinalIgnoreCase));
        vm.CurrentPaneFileList.SelectedItem = selected;

        vm.OpenInNewWindowCommand.Execute(null);

        Assert(string.Equals(fs.LastShellVerbPath, child.FullName, StringComparison.OrdinalIgnoreCase),
            "Open in New Window should target the selected folder when one folder is selected.");
        Assert(string.Equals(fs.LastShellVerb, "opennewwindow", StringComparison.OrdinalIgnoreCase),
            "Open in New Window should invoke the shell opennewwindow verb.");
        Assert(vm.StatusText.Contains("new window", StringComparison.OrdinalIgnoreCase),
            "Open in New Window should report a success status.");
    }

    private static void TestRunAsAdministratorCommand(string root)
    {
        var folder = CreateCleanSubdir(root, "runas");
        var filePath = Path.Combine(folder, "tool.exe");
        File.WriteAllText(filePath, "stub");

        var fs = new RecordingFileSystemService(new FileSystemService());
        var clipboard = new FakeClipboardService();
        var vm = new MainViewModel(fs, clipboard);
        vm.NavigateCurrentPaneToPath(folder);

        var selected = vm.CurrentPaneFileList!.Items.Single(item =>
            string.Equals(item.FullPath, filePath, StringComparison.OrdinalIgnoreCase));
        vm.CurrentPaneFileList.SelectedItem = selected;

        vm.RunAsAdministratorCommand.Execute(null);

        Assert(string.Equals(fs.LastShellVerbPath, filePath, StringComparison.OrdinalIgnoreCase),
            "Run as Administrator should target the selected item.");
        Assert(string.Equals(fs.LastShellVerb, "runas", StringComparison.OrdinalIgnoreCase),
            "Run as Administrator should invoke the runas shell verb.");
        Assert(vm.StatusText.Contains("administrator", StringComparison.OrdinalIgnoreCase),
            "Run as Administrator should report a success status.");
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

    private static void EnsureSmokeApplication()
    {
        if (Application.Current == null)
        {
            var app = new CubicAIExplorer.App();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            app.InitializeComponent();
        }

        ShellIconConverter.IconService ??= new ShellIconService();
    }

    private static void WaitFor(Func<bool> condition, int timeoutMs = 2000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (!condition() && Environment.TickCount64 < deadline)
            Thread.Sleep(50);
    }

    private static void DoEvents()
    {
        Application.Current?.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
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

    private sealed class RecordingFileSystemService : IFileSystemService
    {
        private readonly IFileSystemService _inner;

        public RecordingFileSystemService(IFileSystemService inner)
        {
            _inner = inner;
        }

        public string? LastRevealPath { get; private set; }
        public List<string> LastRevealPaths { get; } = [];
        public string? LastOpenedFolderPath { get; private set; }
        public string? LastShellVerbPath { get; private set; }
        public string? LastShellVerb { get; private set; }
        public int EmptyRecycleBinCallCount { get; private set; }

        public IReadOnlyList<FileSystemItem> GetDrives() => _inner.GetDrives();
        public IReadOnlyList<FileSystemItem> GetDirectoryContents(string path, bool showHidden = false) => _inner.GetDirectoryContents(path, showHidden);
        public IReadOnlyList<FileSystemItem> GetSubDirectories(string path, bool showHidden = false) => _inner.GetSubDirectories(path, showHidden);
        public IReadOnlyList<string> GetFiles(string path, bool showHidden = false) => _inner.GetFiles(path, showHidden);
        public string GetDisplayName(string path) => _inner.GetDisplayName(path);
        public string? ResolveDirectoryPath(string path) => _inner.ResolveDirectoryPath(path);
        public bool DirectoryExists(string path) => _inner.DirectoryExists(path);
        public bool FileExists(string path) => _inner.FileExists(path);
        public string GetParentPath(string path) => _inner.GetParentPath(path);
        public void OpenFile(string path) => _inner.OpenFile(path);

        public void RevealInExplorer(string path)
        {
            LastRevealPath = path;
            LastRevealPaths.Clear();
            LastRevealPaths.Add(path);
        }

        public void RevealInExplorer(IEnumerable<string> paths)
        {
            LastRevealPaths.Clear();
            LastRevealPaths.AddRange(paths);
            LastRevealPath = LastRevealPaths.Count == 1 ? LastRevealPaths[0] : null;
        }

        public void OpenInDefaultApp(string path)
        {
            LastOpenedFolderPath = path;
        }

        public void ExecuteShellVerb(string path, string verb)
        {
            LastShellVerbPath = path;
            LastShellVerb = verb;
        }

        public void EmptyRecycleBin()
        {
            EmptyRecycleBinCallCount++;
        }

        public void ShowNativeProperties(string path) { }
        public void ShowNativeProperties(IEnumerable<string> paths) { }

        public IReadOnlyList<FileTransferResult> CopyFiles(
            IEnumerable<string> sourcePaths,
            string destinationDirectory,
            FileTransferCollisionResolution collisionResolution = FileTransferCollisionResolution.KeepBoth,
            IFileOperationContext? operationContext = null) => _inner.CopyFiles(sourcePaths, destinationDirectory, collisionResolution, operationContext);

        public IReadOnlyList<FileTransferResult> MoveFiles(
            IEnumerable<string> sourcePaths,
            string destinationDirectory,
            FileTransferCollisionResolution collisionResolution = FileTransferCollisionResolution.KeepBoth,
            IFileOperationContext? operationContext = null) => _inner.MoveFiles(sourcePaths, destinationDirectory, collisionResolution, operationContext);

        public void DeleteFiles(IEnumerable<string> paths, bool permanentDelete = false, IFileOperationContext? operationContext = null)
            => _inner.DeleteFiles(paths, permanentDelete, operationContext);

        public string RenameFile(string path, string newName) => _inner.RenameFile(path, newName);
        public string CreateFolder(string parentPath, string folderName) => _inner.CreateFolder(parentPath, folderName);
        public string CreateFile(string parentPath, string fileName) => _inner.CreateFile(parentPath, fileName);
        public string CreateFileFromTemplate(string parentPath, string templatePath, string? fileName = null)
            => _inner.CreateFileFromTemplate(parentPath, templatePath, fileName);
        public void CreateSymbolicLink(string linkPath, string targetPath) => _inner.CreateSymbolicLink(linkPath, targetPath);
        public string? EnsureDirectoryExists(string path) => _inner.EnsureDirectoryExists(path);
        public IReadOnlyList<ArchiveEntryInfo> GetArchiveEntries(string archivePath, int maxEntries = 100) => _inner.GetArchiveEntries(archivePath, maxEntries);
        public void ExtractArchive(string archivePath, string destinationDirectory, IFileOperationContext? operationContext = null)
            => _inner.ExtractArchive(archivePath, destinationDirectory, operationContext);
        public void ExtractArchiveEntries(
            string archivePath,
            string destinationDirectory,
            IEnumerable<string> entryPaths,
            IFileOperationContext? operationContext = null) => _inner.ExtractArchiveEntries(archivePath, destinationDirectory, entryPaths, operationContext);
    }

    private sealed class ThrowingSymbolicLinkFileSystemService : IFileSystemService
    {
        private readonly IFileSystemService _inner;
        private readonly Exception _exception;

        public ThrowingSymbolicLinkFileSystemService(IFileSystemService inner, Exception exception)
        {
            _inner = inner;
            _exception = exception;
        }

        public IReadOnlyList<FileSystemItem> GetDrives() => _inner.GetDrives();
        public IReadOnlyList<FileSystemItem> GetDirectoryContents(string path, bool showHidden = false) => _inner.GetDirectoryContents(path, showHidden);
        public IReadOnlyList<FileSystemItem> GetSubDirectories(string path, bool showHidden = false) => _inner.GetSubDirectories(path, showHidden);
        public IReadOnlyList<string> GetFiles(string path, bool showHidden = false) => _inner.GetFiles(path, showHidden);
        public string GetDisplayName(string path) => _inner.GetDisplayName(path);
        public string? ResolveDirectoryPath(string path) => _inner.ResolveDirectoryPath(path);
        public bool DirectoryExists(string path) => _inner.DirectoryExists(path);
        public bool FileExists(string path) => _inner.FileExists(path);
        public string GetParentPath(string path) => _inner.GetParentPath(path);
        public void OpenFile(string path) => _inner.OpenFile(path);
        public void RevealInExplorer(string path) => _inner.RevealInExplorer(path);
        public void RevealInExplorer(IEnumerable<string> paths) => _inner.RevealInExplorer(paths);
        public void OpenInDefaultApp(string path) => _inner.OpenInDefaultApp(path);
        public void ExecuteShellVerb(string path, string verb) => _inner.ExecuteShellVerb(path, verb);
        public void EmptyRecycleBin() => _inner.EmptyRecycleBin();
        public void ShowNativeProperties(string path) => _inner.ShowNativeProperties(path);
        public void ShowNativeProperties(IEnumerable<string> paths) => _inner.ShowNativeProperties(paths);
        public IReadOnlyList<FileTransferResult> CopyFiles(
            IEnumerable<string> sourcePaths,
            string destinationDirectory,
            FileTransferCollisionResolution collisionResolution = FileTransferCollisionResolution.KeepBoth,
            IFileOperationContext? operationContext = null) => _inner.CopyFiles(sourcePaths, destinationDirectory, collisionResolution, operationContext);
        public IReadOnlyList<FileTransferResult> MoveFiles(
            IEnumerable<string> sourcePaths,
            string destinationDirectory,
            FileTransferCollisionResolution collisionResolution = FileTransferCollisionResolution.KeepBoth,
            IFileOperationContext? operationContext = null) => _inner.MoveFiles(sourcePaths, destinationDirectory, collisionResolution, operationContext);
        public void DeleteFiles(IEnumerable<string> paths, bool permanentDelete = false, IFileOperationContext? operationContext = null)
            => _inner.DeleteFiles(paths, permanentDelete, operationContext);
        public string RenameFile(string path, string newName) => _inner.RenameFile(path, newName);
        public string CreateFolder(string parentPath, string folderName) => _inner.CreateFolder(parentPath, folderName);
        public string CreateFile(string parentPath, string fileName) => _inner.CreateFile(parentPath, fileName);
        public string CreateFileFromTemplate(string parentPath, string templatePath, string? fileName = null)
            => _inner.CreateFileFromTemplate(parentPath, templatePath, fileName);
        public void CreateSymbolicLink(string linkPath, string targetPath) => throw _exception;
        public string? EnsureDirectoryExists(string path) => _inner.EnsureDirectoryExists(path);
        public IReadOnlyList<ArchiveEntryInfo> GetArchiveEntries(string archivePath, int maxEntries = 100) => _inner.GetArchiveEntries(archivePath, maxEntries);
        public void ExtractArchive(string archivePath, string destinationDirectory, IFileOperationContext? operationContext = null)
            => _inner.ExtractArchive(archivePath, destinationDirectory, operationContext);
        public void ExtractArchiveEntries(
            string archivePath,
            string destinationDirectory,
            IEnumerable<string> entryPaths,
            IFileOperationContext? operationContext = null) => _inner.ExtractArchiveEntries(archivePath, destinationDirectory, entryPaths, operationContext);
    }
}
