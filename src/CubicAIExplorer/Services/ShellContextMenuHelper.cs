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
    private const uint GCS_VERBW = 0x00000004;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHGetDesktopFolder(out IShellFolder ppshf);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hWnd, IntPtr lpcpm);

    public static bool ShowContextMenu(IntPtr hWnd, List<string> paths, int x, int y)
    {
        if (paths.Count == 0) return false;

        IShellFolder? desktop = null;
        IShellFolder? parentFolder = null;
        IntPtr pidlParent = IntPtr.Zero;
        IntPtr pidlFull = IntPtr.Zero;
        IntPtr[]? pidls = null;
        IContextMenu? contextMenu = null;
        IntPtr hMenu = IntPtr.Zero;

        try
        {
            if (SHGetDesktopFolder(out desktop) != 0 || desktop == null) return false;

            // For simplicity in this implementation, we'll handle the first item's parent.
            // In a full implementation, we'd need to ensure all items share the same parent.
            string firstPath = paths[0];
            string? parentPath = Path.GetDirectoryName(firstPath);
            string[] names = paths.Select(Path.GetFileName).Where(n => n != null).Cast<string>().ToArray();

            if (string.IsNullOrEmpty(parentPath))
            {
                // Desktop or root
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
                    // Clean up already allocated pidls
                    for (int j = 0; j < i; j++) Marshal.FreeCoTaskMem(pidls[j]);
                    return false;
                }
            }

            Guid guidIContextMenu = typeof(IContextMenu).GUID;
            if (parentFolder.GetUIObjectOf(hWnd, (uint)pidls.Length, pidls, ref guidIContextMenu, IntPtr.Zero, out var pvContextMenu) != 0)
                return false;

            contextMenu = (IContextMenu)pvContextMenu;

            hMenu = CreatePopupMenu();
            if (hMenu == IntPtr.Zero) return false;

            if (contextMenu.QueryContextMenu(hMenu, 0, 1, 0x7FFF, CMF_NORMAL) < 0)
                return false;

            uint command = (uint)TrackPopupMenuEx(hMenu, TPM_LEFTALIGN | TPM_RETURNCMD | TPM_RIGHTBUTTON, x, y, hWnd, IntPtr.Zero);

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
