using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public static class ShellPropertyHelper
{
    private static readonly Guid IPropertyStoreGuid = new Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99");

    public static readonly PROPERTYKEY PKEY_Company = new PROPERTYKEY(new Guid("0CEF7D53-FA64-11D1-A203-0000F81FEDEE"), 3);
    public static readonly PROPERTYKEY PKEY_Copyright = new PROPERTYKEY(new Guid("64440492-4C8B-11D1-8B70-080036B11A03"), 11);
    public static readonly PROPERTYKEY PKEY_FileVersion = new PROPERTYKEY(new Guid("0CEF7D53-FA64-11D1-A203-0000F81FEDEE"), 4);
    public static readonly PROPERTYKEY PKEY_FileDescription = new PROPERTYKEY(new Guid("0CEF7D53-FA64-11D1-A203-0000F81FEDEE"), 2);
    public static readonly PROPERTYKEY PKEY_Image_Dimensions = new PROPERTYKEY(new Guid("64440492-4C8B-11D1-8B70-080036B11A03"), 13);
    public static readonly PROPERTYKEY PKEY_Media_Duration = new PROPERTYKEY(new Guid("64440492-4C8B-11D1-8B70-080036B11A03"), 3);

    public static ShellProperties GetShellProperties(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return ShellProperties.Empty;

        try
        {
            var propertyStoreGuid = IPropertyStoreGuid;
            int hr = SHGetPropertyStoreFromParsingName(filePath, IntPtr.Zero, GETPROPERTYSTOREFLAGS.GPS_FASTPROPERTIESONLY, ref propertyStoreGuid, out IPropertyStore store);

            
            if (hr == 0 && store != null)
            {
                try
                {
                    return new ShellProperties
                    {
                        Company = GetString(store, PKEY_Company),
                        Copyright = GetString(store, PKEY_Copyright),
                        FileVersion = GetString(store, PKEY_FileVersion),
                        FileDescription = GetString(store, PKEY_FileDescription),
                        Dimensions = GetString(store, PKEY_Image_Dimensions),
                        Duration = GetDurationString(store, PKEY_Media_Duration)
                    };
                }
                finally
                {
                    Marshal.ReleaseComObject(store);
                }
            }
        }
        catch
        {
            // Ignore
        }

        return ShellProperties.Empty;
    }

    private static string? GetString(IPropertyStore store, PROPERTYKEY key)
    {
        IntPtr pv = Marshal.AllocCoTaskMem(24);
        ZeroMemory(pv, 24);
        try
        {
            if (store.GetValue(ref key, pv) == 0)
            {
                return PropVariantToString(pv);
            }
        }
        catch { }
        finally
        {
            PropVariantClear(pv);
            Marshal.FreeCoTaskMem(pv);
        }
        return null;
    }

    private static string? GetDurationString(IPropertyStore store, PROPERTYKEY key)
    {
        IntPtr pv = Marshal.AllocCoTaskMem(24);
        ZeroMemory(pv, 24);
        try
        {
            if (store.GetValue(ref key, pv) == 0)
            {
                ushort vt = (ushort)Marshal.ReadInt16(pv);
                if (vt == 21) // VT_UI8
                {
                    long ticks = Marshal.ReadInt64(pv, 8);
                    var ts = TimeSpan.FromTicks(ticks);
                    if (ts.TotalHours >= 1)
                        return ts.ToString(@"h\:mm\:ss");
                    return ts.ToString(@"m\:ss");
                }
                return PropVariantToString(pv);
            }
        }
        catch { }
        finally
        {
            PropVariantClear(pv);
            Marshal.FreeCoTaskMem(pv);
        }
        return null;
    }

    private static string? PropVariantToString(IntPtr pv)
    {
        ushort vt = (ushort)Marshal.ReadInt16(pv);
        if (vt == 0) return null;

        IntPtr ptr = Marshal.ReadIntPtr(pv, 8);
        switch (vt)
        {
            case 31: // VT_LPWSTR
                return ptr != IntPtr.Zero ? Marshal.PtrToStringUni(ptr) : null;
            case 8: // VT_BSTR
                return ptr != IntPtr.Zero ? Marshal.PtrToStringBSTR(ptr) : null;
            case 20: // VT_I8
                return Marshal.ReadInt64(pv, 8).ToString();
            case 21: // VT_UI8
                return ((ulong)Marshal.ReadInt64(pv, 8)).ToString();
            case 3: // VT_I4
                return Marshal.ReadInt32(pv, 8).ToString();
            case 19: // VT_UI4
                return ((uint)Marshal.ReadInt32(pv, 8)).ToString();
            default:
                return null;
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int SHGetPropertyStoreFromParsingName(
        [In, MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        [In] IntPtr pbc,
        [In] GETPROPERTYSTOREFLAGS flags,
        [In] ref Guid riid,
        [Out, MarshalAs(UnmanagedType.Interface)] out IPropertyStore ppv);

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(IntPtr pvar);

    [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory")]
    private static extern void ZeroMemory(IntPtr dest, int size);

    [ComImport, Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint cProps);
        [PreserveSig] int GetAt(uint iProp, out PROPERTYKEY pkey);
        [PreserveSig] int GetValue(ref PROPERTYKEY key, IntPtr pv);
        [PreserveSig] int SetValue(ref PROPERTYKEY key, IntPtr pv);
        [PreserveSig] int Commit();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;
        public PROPERTYKEY(Guid guid, uint id) { fmtid = guid; pid = id; }
    }

    public enum GETPROPERTYSTOREFLAGS
    {
        GPS_DEFAULT = 0,
        GPS_HANDLERPROPERTIESONLY = 0x00000001,
        GPS_READWRITE = 0x00000002,
        GPS_TEMPORARY = 0x00000004,
        GPS_FASTPROPERTIESONLY = 0x00000008,
        GPS_OPENFORQUERY = 0x00000010,
        GPS_DELAYCREATION = 0x00000020,
        GPS_BESTEFFORT = 0x00000040,
        GPS_NO_OPLOCK = 0x00000080,
        GPS_MASK_VALID = 0x000000FF
    }
}
