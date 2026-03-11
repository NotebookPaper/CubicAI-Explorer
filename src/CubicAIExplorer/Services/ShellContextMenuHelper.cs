using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CubicAIExplorer.Services;

public static class ShellContextMenuHelper
{
    private const uint TPM_LEFTALIGN = 0x0000;
    private const uint TPM_RETURNCMD = 0x0100;
    private const uint TPM_RIGHTBUTTON = 0x0002;

    private const uint CMF_NORMAL = 0x00000000;
    
    private const uint WM_INITMENUPOPUP = 0x0117;
    private const uint WM_DRAWITEM = 0x002B;
    private const uint WM_MEASUREITEM = 0x002C;
    private const uint WM_MENUCHAR = 0x0120;
    private const uint WM_MENUSELECT = 0x011F;

    private static IContextMenu? _currentContextMenu;
    private static IContextMenu2? _currentContextMenu2;
    private static IContextMenu3? _currentContextMenu3;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHGetDesktopFolder(out IShellFolder ppshf);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hWnd, IntPtr lpcpm);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int GWLP_WNDPROC = -4;
    private static IntPtr _oldWndProc = IntPtr.Zero;
    private static WndProcDelegate? _wndProcDelegate;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public static bool ShowBackgroundContextMenu(IntPtr hWnd, string folderPath, int x, int y)
    {
        if (string.IsNullOrEmpty(folderPath)) return false;

        IShellFolder? desktop = null;
        IShellFolder? folder = null;
        IntPtr pidlFolder = IntPtr.Zero;
        IContextMenu? contextMenu = null;
        IntPtr hMenu = IntPtr.Zero;

        try
        {
            if (SHGetDesktopFolder(out desktop) != 0 || desktop == null) return false;

            uint pchEaten = 0;
            uint pdwAttributes = 0;
            if (desktop.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, folderPath, ref pchEaten, out pidlFolder, ref pdwAttributes) != 0)
                return false;

            Guid guidIShellFolder = typeof(IShellFolder).GUID;
            if (desktop.BindToObject(pidlFolder, IntPtr.Zero, ref guidIShellFolder, out var pv) != 0)
                return false;

            folder = (IShellFolder)pv;

            Guid guidIContextMenu = typeof(IContextMenu).GUID;
            // CreateViewObject with IID_IContextMenu on a folder gets the background context menu
            if (folder.CreateViewObject(hWnd, ref guidIContextMenu, out var pvContextMenu) != 0)
                return false;

            contextMenu = (IContextMenu)Marshal.GetObjectForIUnknown(pvContextMenu);
            _currentContextMenu = contextMenu;
            _currentContextMenu2 = contextMenu as IContextMenu2;
            _currentContextMenu3 = contextMenu as IContextMenu3;

            hMenu = CreatePopupMenu();
            if (hMenu == IntPtr.Zero) return false;

            if (contextMenu.QueryContextMenu(hMenu, 0, 1, 0x7FFF, CMF_NORMAL) < 0)
                return false;

            // Subclass the window to handle context menu messages
            _wndProcDelegate = new WndProcDelegate(HookWndProc);
            _oldWndProc = SetWindowLongPtr(hWnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));

            uint command = (uint)TrackPopupMenuEx(hMenu, TPM_LEFTALIGN | TPM_RETURNCMD | TPM_RIGHTBUTTON, x, y, hWnd, IntPtr.Zero);

            // Un-subclass
            if (_oldWndProc != IntPtr.Zero)
            {
                SetWindowLongPtr(hWnd, GWLP_WNDPROC, _oldWndProc);
                _oldWndProc = IntPtr.Zero;
                _wndProcDelegate = null;
            }

            if (command > 0)
            {
                CMINVOKECOMMANDINFOEX invoke = new CMINVOKECOMMANDINFOEX();
                invoke.cbSize = (uint)Marshal.SizeOf(invoke);
                invoke.hwnd = hWnd;
                invoke.lpVerb = (IntPtr)(command - 1);
                invoke.nShow = 1; // SW_SHOWNORMAL

                contextMenu.InvokeCommand(ref invoke);
            }

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _currentContextMenu = null;
            _currentContextMenu2 = null;
            _currentContextMenu3 = null;

            if (hMenu != IntPtr.Zero) DestroyMenu(hMenu);
            if (contextMenu != null) Marshal.ReleaseComObject(contextMenu);
            if (pidlFolder != IntPtr.Zero) Marshal.FreeCoTaskMem(pidlFolder);
            if (folder != null && folder != desktop) Marshal.ReleaseComObject(folder);
            if (desktop != null) Marshal.ReleaseComObject(desktop);
        }
    }

    public static bool ShowContextMenu(IntPtr hWnd, List<string> paths, int x, int y)
    {
        if (paths.Count == 0) return false;

        IShellFolder? desktop = null;
        IShellFolder? parentFolder = null;
        IntPtr pidlParent = IntPtr.Zero;
        IntPtr[]? pidls = null;
        IContextMenu? contextMenu = null;
        IntPtr hMenu = IntPtr.Zero;

        try
        {
            if (SHGetDesktopFolder(out desktop) != 0 || desktop == null) return false;

            string firstPath = paths[0];
            string? parentPath = Path.GetDirectoryName(firstPath);
            string[] names = paths.Select(Path.GetFileName).Where(n => n != null).Cast<string>().ToArray();

            if (string.IsNullOrEmpty(parentPath))
            {
                parentFolder = desktop;
            }
            else
            {
                uint pchEaten = 0;
                uint pdwAttributes = 0;
                if (desktop.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, parentPath, ref pchEaten, out pidlParent, ref pdwAttributes) != 0)
                    return false;

                Guid guidIShellFolder = typeof(IShellFolder).GUID;
                if (desktop.BindToObject(pidlParent, IntPtr.Zero, ref guidIShellFolder, out var pv) != 0)
                    return false;
                
                parentFolder = (IShellFolder)pv;
            }

            pidls = new IntPtr[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                uint pchEaten = 0;
                uint pdwAttributes = 0;
                if (parentFolder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, names[i], ref pchEaten, out pidls[i], ref pdwAttributes) != 0)
                {
                    for (int j = 0; j < i; j++) Marshal.FreeCoTaskMem(pidls[j]);
                    return false;
                }
            }

            Guid guidIContextMenu = typeof(IContextMenu).GUID;
            if (parentFolder.GetUIObjectOf(hWnd, (uint)pidls.Length, pidls, ref guidIContextMenu, IntPtr.Zero, out var pvContextMenu) != 0)
                return false;

            contextMenu = (IContextMenu)pvContextMenu;
            _currentContextMenu = contextMenu;
            _currentContextMenu2 = contextMenu as IContextMenu2;
            _currentContextMenu3 = contextMenu as IContextMenu3;

            hMenu = CreatePopupMenu();
            if (hMenu == IntPtr.Zero) return false;

            if (contextMenu.QueryContextMenu(hMenu, 0, 1, 0x7FFF, CMF_NORMAL) < 0)
                return false;

            // Subclass the window to handle context menu messages
            _wndProcDelegate = new WndProcDelegate(HookWndProc);
            _oldWndProc = SetWindowLongPtr(hWnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));

            uint command = (uint)TrackPopupMenuEx(hMenu, TPM_LEFTALIGN | TPM_RETURNCMD | TPM_RIGHTBUTTON, x, y, hWnd, IntPtr.Zero);

            // Un-subclass
            if (_oldWndProc != IntPtr.Zero)
            {
                SetWindowLongPtr(hWnd, GWLP_WNDPROC, _oldWndProc);
                _oldWndProc = IntPtr.Zero;
                _wndProcDelegate = null;
            }

            if (command > 0)
            {
                CMINVOKECOMMANDINFOEX invoke = new CMINVOKECOMMANDINFOEX();
                invoke.cbSize = (uint)Marshal.SizeOf(invoke);
                invoke.hwnd = hWnd;
                invoke.lpVerb = (IntPtr)(command - 1);
                invoke.nShow = 1; // SW_SHOWNORMAL

                contextMenu.InvokeCommand(ref invoke);
            }

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _currentContextMenu = null;
            _currentContextMenu2 = null;
            _currentContextMenu3 = null;

            if (hMenu != IntPtr.Zero) DestroyMenu(hMenu);
            if (contextMenu != null) Marshal.ReleaseComObject(contextMenu);
            if (pidls != null)
            {
                foreach (var pidl in pidls) if (pidl != IntPtr.Zero) Marshal.FreeCoTaskMem(pidl);
            }
            if (pidlParent != IntPtr.Zero) Marshal.FreeCoTaskMem(pidlParent);
            if (parentFolder != null && parentFolder != desktop) Marshal.ReleaseComObject(parentFolder);
            if (desktop != null) Marshal.ReleaseComObject(desktop);
        }
    }

    private static IntPtr HookWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (_currentContextMenu3 != null)
        {
            if (_currentContextMenu3.HandleMenuMsg2(msg, wParam, lParam, out var lResult) == 0)
                return lResult;
        }
        else if (_currentContextMenu2 != null)
        {
            if (_currentContextMenu2.HandleMenuMsg(msg, wParam, lParam) == 0)
                return IntPtr.Zero;
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214E6-0000-0000-C000-000000000046")]
    private interface IShellFolder
    {
        [PreserveSig]
        int ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, ref uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
        [PreserveSig]
        int EnumObjects(IntPtr hwnd, uint grfFlags, out IntPtr ppenumIDList);
        [PreserveSig]
        int BindToObject(IntPtr pidl, IntPtr pbc, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
        [PreserveSig]
        int BindToStorage(IntPtr pidl, IntPtr pbc, [In] ref Guid riid, out IntPtr ppv);
        [PreserveSig]
        int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
        [PreserveSig]
        int CreateViewObject(IntPtr hwndOwner, [In] ref Guid riid, out IntPtr ppv);
        [PreserveSig]
        int GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, ref uint rgfInOut);
        [PreserveSig]
        int GetUIObjectOf(IntPtr hwndOwner, uint cidl, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, [In] ref Guid riid, IntPtr rgfReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
        [PreserveSig]
        int GetDisplayNameOf(IntPtr pidl, uint uFlags, out STRRET pName);
        [PreserveSig]
        int SetNameOf(IntPtr hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string pszName, uint uFlags, out IntPtr ppidlOut);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214E4-0000-0000-C000-000000000046")]
    private interface IContextMenu
    {
        [PreserveSig]
        int QueryContextMenu(IntPtr hmenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);
        [PreserveSig]
        int InvokeCommand(ref CMINVOKECOMMANDINFOEX pici);
        [PreserveSig]
        int GetCommandString(UIntPtr idCmd, uint uType, IntPtr pReserved, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, uint cchMax);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214f4-0000-0000-c000-000000000046")]
    private interface IContextMenu2 : IContextMenu
    {
        [PreserveSig]
        int HandleMenuMsg(uint uMsg, IntPtr wParam, IntPtr lParam);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("bcfce0a0-182d-11d1-b350-00a0c9055d8e")]
    private interface IContextMenu3 : IContextMenu2
    {
        [PreserveSig]
        int HandleMenuMsg2(uint uMsg, IntPtr wParam, IntPtr lParam, out IntPtr plResult);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CMINVOKECOMMANDINFOEX
    {
        public uint cbSize;
        public uint fMask;
        public IntPtr hwnd;
        public IntPtr lpVerb;
        public IntPtr lpParameters;
        public IntPtr lpDirectory;
        public int nShow;
        public uint dwHotKey;
        public IntPtr hIcon;
        public IntPtr lpTitle;
        public IntPtr lpVerbW;
        public IntPtr lpParametersW;
        public IntPtr lpDirectoryW;
        public IntPtr lpTitleW;
        public POINT ptInvoke;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STRRET
    {
        public uint uType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
        public byte[] data;
    }
}
