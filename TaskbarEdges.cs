using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace StandUp {
    internal static class TaskbarEdges {
        const uint GetTaskbarPosition = 5, GetAutoHideBarForMonitor = 11;
        const string PrimaryBar = "Shell_TrayWnd", SecondaryBar = "Shell_SecondaryTrayWnd";
        static readonly AnchorStyles[] ShellEdges = { AnchorStyles.Left, AnchorStyles.Top, AnchorStyles.Right, AnchorStyles.Bottom };

        [StructLayout(LayoutKind.Sequential)]
        struct Rect {
            public int Left, Top, Right, Bottom;
            public Rect(Rectangle r) { Left=r.Left; Top=r.Top; Right=r.Right; Bottom=r.Bottom; }
            public Rectangle Bounds { get { return Rectangle.FromLTRB(Left, Top, Right, Bottom); } }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct AppBarData {
            public uint Size;
            public IntPtr Window;
            public uint Callback, Edge;
            public Rect Area;
            public IntPtr Parameter;
        }

        [DllImport("shell32.dll")] static extern IntPtr SHAppBarMessage(uint message, ref AppBarData data);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string title);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr window, StringBuilder name, int length);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window, out Rect area);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);

        static IntPtr AutoHideOwner(Rectangle bounds, uint edge) {
            AppBarData data=new AppBarData { Size=(uint)Marshal.SizeOf(typeof(AppBarData)), Edge=edge, Area=new Rect(bounds) };
            return SHAppBarMessage(GetAutoHideBarForMonitor,ref data);
        }

        internal static AnchorStyles OnScreen(Screen display) {
            Rectangle bounds=display.Bounds, work=display.WorkingArea;
            AppBarData data=new AppBarData { Size=(uint)Marshal.SizeOf(typeof(AppBarData)) };

            // The per-monitor registration still identifies a retracted taskbar. Other
            // applications can register appbars too, so verify the returned window.
            for(uint edge=0; edge<ShellEdges.Length; edge++) {
                IntPtr bar=AutoHideOwner(bounds,edge);
                if(bar==IntPtr.Zero) continue;
                StringBuilder name=new StringBuilder(64);
                GetClassName(bar,name,name.Capacity);
                string className=name.ToString();
                if(className==PrimaryBar || className==SecondaryBar) return ShellEdges[edge];
            }

            // This shell query describes the main taskbar only. Explorer owns a
            // separate top-level window on each secondary monitor that has a taskbar.
            IntPtr primary=FindWindowEx(IntPtr.Zero,IntPtr.Zero,PrimaryBar,null);
            AnchorStyles found;
            if(SHAppBarMessage(GetTaskbarPosition,ref data)!=IntPtr.Zero)
                found=EdgeOf(bounds,work,data.Area.Bounds);
            else
                found=WindowEdge(bounds,work,primary);
            if(found!=AnchorStyles.None && RegisteredElsewhere(display,primary)) found=AnchorStyles.None;
            IntPtr secondary=IntPtr.Zero;
            while((secondary=FindWindowEx(IntPtr.Zero,secondary,SecondaryBar,null))!=IntPtr.Zero) {
                AnchorStyles side=WindowEdge(bounds,work,secondary);
                if(side!=AnchorStyles.None && !RegisteredElsewhere(display,secondary)) found|=side;
            }
            return found;
        }

        static bool RegisteredElsewhere(Screen display, IntPtr taskbar) {
            // A retracting taskbar can briefly occupy pixels on the adjacent monitor.
            if(taskbar==IntPtr.Zero) return false;
            foreach(Screen other in Screen.AllScreens) {
                if(other.Bounds==display.Bounds) continue;
                for(uint edge=0; edge<ShellEdges.Length; edge++)
                    if(AutoHideOwner(other.Bounds,edge)==taskbar) return true;
            }
            return false;
        }

        static AnchorStyles WindowEdge(Rectangle bounds, Rectangle work, IntPtr window) {
            Rect rect;
            if(window==IntPtr.Zero || !IsWindowVisible(window) || !GetWindowRect(window,out rect))
                return AnchorStyles.None;
            return EdgeOf(bounds,work,rect.Bounds);
        }

        internal static AnchorStyles EdgeOf(Rectangle bounds, Rectangle work, Rectangle bar) {
            Rectangle clip=Rectangle.Intersect(bounds,bar);
            if(clip.Width<=0 || clip.Height<=0 || !bounds.Contains(bar.Left+bar.Width/2,bar.Top+bar.Height/2))
                return AnchorStyles.None;
            // A reserved inset alone may belong to any toolbar. It only corroborates
            // the location of a known taskbar (including one stacked with a toolbar).
            const int tolerance=2;
            if(bar.Width>bar.Height && clip.Width>=bounds.Width/2 && bar.Height<=bounds.Height/2) {
                if(bar.Top<=bounds.Top+tolerance || bar.Bottom<=work.Top+tolerance) return AnchorStyles.Top;
                if(bar.Bottom>=bounds.Bottom-tolerance || bar.Top>=work.Bottom-tolerance) return AnchorStyles.Bottom;
            }
            if(bar.Height>bar.Width && clip.Height>=bounds.Height/2 && bar.Width<=bounds.Width/2) {
                if(bar.Left<=bounds.Left+tolerance || bar.Right<=work.Left+tolerance) return AnchorStyles.Left;
                if(bar.Right>=bounds.Right-tolerance || bar.Left>=work.Right-tolerance) return AnchorStyles.Right;
            }
            return AnchorStyles.None;
        }
    }
}
