using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Flow.Launcher.Plugin.OneNote
{
    //https://stackoverflow.com/questions/2636721/bring-another-processes-window-to-foreground-when-it-has-showintaskbar-false
    public static partial class WindowHelper
    {
        public static void FocusOneNote()
        {
            var process = Process.GetProcessesByName("onenote").FirstOrDefault();
            if (process == null) 
                return;
            IntPtr handle = process.MainWindowHandle;
            if (IsIconic(handle))
            {
                ShowWindow(handle, SW_RESTORE);
            }

            SetForegroundWindow(handle);
        }
        const int SW_RESTORE = 9;

        [LibraryImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetForegroundWindow(IntPtr handle);
        
        [LibraryImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ShowWindow(IntPtr handle, int nCmdShow);
        
        [LibraryImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsIconic(IntPtr handle);
    }
}