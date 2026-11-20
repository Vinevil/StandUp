using System;
using System.IO;
using System.Runtime.InteropServices;

namespace StandUp {
    internal static class TaskbarIdentity {
        const string AppId = "StandUp.Portable.DesktopReminder";
        const string IconFileName = "standup-clock-bold.ico";
        static readonly Guid AppModel = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
        const uint RelaunchCommand = 2, RelaunchIcon = 3, DisplayName = 4, ApplicationId = 5;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct PropertyKey {
            public Guid Format;
            public uint Id;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PropVariant {
            public ushort Type, Reserved1, Reserved2, Reserved3;
            public IntPtr Value, UnionPadding;
        }

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IPropertyStore {
            [PreserveSig] int GetCount(out uint count);
            [PreserveSig] int GetAt(uint index, out PropertyKey key);
            [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
            [PreserveSig] int SetValue(ref PropertyKey key, ref PropVariant value);
            [PreserveSig] int Commit();
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        static extern int SetCurrentProcessExplicitAppUserModelID(string id);

        [DllImport("shell32.dll", ExactSpelling = true)]
        static extern int SHGetPropertyStoreForWindow(IntPtr hwnd, ref Guid iid, out IPropertyStore store);

        internal static void SetProcessId() {
            SetCurrentProcessExplicitAppUserModelID(AppId);
        }

        // These window properties are also the Shell's source when it rebuilds a taskbar group.
        // Publish the identity last, once the launch command, name and icon are available together.
        internal static void Bind(IntPtr window) {
            Guid iid = typeof(IPropertyStore).GUID;
            IPropertyStore store;
            if (SHGetPropertyStoreForWindow(window, ref iid, out store) < 0) return;
            try {
                string exe = typeof(TaskbarIdentity).Assembly.Location;
                string icon = Path.Combine(Path.GetDirectoryName(exe), IconFileName);
                // The C# compiler embeds the app's RT_GROUP_ICON under resource ID 32512.
                string resource = File.Exists(icon) ? icon + ",0" : exe + ",-32512";
                Set(store, RelaunchCommand, "\"" + exe + "\"");
                Set(store, DisplayName, "站起来 · 桌面小憩");
                Set(store, RelaunchIcon, resource);
                Set(store, ApplicationId, AppId);
            } finally { Marshal.ReleaseComObject(store); }
        }

        internal static void Unbind(IntPtr window) {
            Guid iid = typeof(IPropertyStore).GUID;
            IPropertyStore store;
            if (SHGetPropertyStoreForWindow(window, ref iid, out store) < 0) return;
            try {
                Set(store, ApplicationId, null);
                Set(store, RelaunchIcon, null);
                Set(store, DisplayName, null);
                Set(store, RelaunchCommand, null);
            } finally { Marshal.ReleaseComObject(store); }
        }

        static void Set(IPropertyStore store, uint id, string text) {
            PropertyKey key = new PropertyKey { Format = AppModel, Id = id };
            // A null value is VT_EMPTY and removes the property before the HWND is destroyed.
            PropVariant value = new PropVariant();
            if (text != null) {
                value.Type = (ushort)VarEnum.VT_LPWSTR;
                value.Value = Marshal.StringToCoTaskMemUni(text);
            }
            try { store.SetValue(ref key, ref value); }
            finally { Marshal.FreeCoTaskMem(value.Value); }
        }
    }
}
