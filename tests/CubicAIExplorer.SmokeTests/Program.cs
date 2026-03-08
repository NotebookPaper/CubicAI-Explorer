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
            Run("multi-select copy command", failures, () => TestMultiSelectCopy(tempRoot));
            Run("drop import copy + move", failures, () => TestDropImport(tempRoot));
            Run("undo move", failures, () => TestUndoMove(tempRoot));
            Run("select-all event", failures, () => TestSelectAllEvent(tempRoot));
            Run("create folder collision suffix", failures, () => TestCreateFolderCollision(tempRoot));
            Run("shell icon service", failures, () => TestShellIconService(tempRoot));
            Run("bookmarks add + dedupe", failures, () => TestBookmarks(tempRoot));
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

    private static void TestXamlWiring()
    {
        var mainXaml = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CubicAIExplorer", "MainWindow.xaml"));
        var appXaml = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CubicAIExplorer", "App.xaml"));
        var main = File.ReadAllText(mainXaml);
        var app = File.ReadAllText(appXaml);

        Assert(main.Contains("Key=\"F2\""), "F2 keybinding should exist.");
        Assert(main.Contains("Key=\"Z\""), "Ctrl+Z keybinding should exist.");
        Assert(main.Contains("Ctrl+Shift"), "Ctrl+Shift+N keybinding should exist.");
        Assert(main.Contains("SelectionMode=\"Extended\""), "Extended selection should exist.");
        Assert(main.Contains("AllowDrop=\"True\""), "File list drag/drop should be enabled.");
        Assert(main.Contains("Drop=\"FileList_Drop\""), "File list drop handler should be wired.");
        Assert(main.Contains("ContextMenuOpening=\"FileList_ContextMenuOpening\""), "Context menu handler should be wired.");
        Assert(main.Contains("StaticResource ShellIconConverter"), "Shell icon converter should be used in MainWindow.");
        Assert(app.Contains("ShellIconConverter"), "ShellIconConverter should be in app resources.");
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
