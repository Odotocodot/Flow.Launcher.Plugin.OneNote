using System;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.OneNote.UI
{
    public class SettingsViewModel : BaseModel
    {
        private readonly OneNoteItemIcons notebookIcons;
        private readonly OneNoteItemIcons sectionIcons;
        public SettingsViewModel(Settings settings, OneNoteItemIcons notebookIcons, OneNoteItemIcons sectionIcons)
        {
            Settings = settings;
            this.notebookIcons = notebookIcons;
            this.sectionIcons = sectionIcons;
        }
        public Settings Settings { get; init; }

        public IEnumerable<int> DefaultRecentCountOptions => Enumerable.Range(1, 16);

        public int CachedIconCount => notebookIcons.CachedIconCount + sectionIcons.CachedIconCount;
        public string CachedIconsSize => GetBytesReadable(notebookIcons.GetIconsFileSize());

        public void ClearCachedIcons()
        {
            notebookIcons.ClearCachedIcons();
            sectionIcons.ClearCachedIcons();
            OnPropertyChanged(nameof(CachedIconsSize));
        }

        // Returns the human-readable file size for an arbitrary, 64-bit file size 
        // The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
        private static string GetBytesReadable(long i)
        {
            // Get absolute value
            long absolute_i = Math.Abs(i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            switch (absolute_i)
            {
                case >= 0x40000000: // Gigabyte 
                    suffix = "GB";
                    readable = i >> 20;
                    break;
                case >= 0x100000: // Megabyte 
                    suffix = "MB";
                    readable = i >> 10;
                    break;
                case >= 0x400:
                    suffix = "KB"; // Kilobyte 
                    readable = i;
                    break;
                default:
                    return i.ToString("0 B"); // Byte
            }
            // Divide by 1024 to get fractional value
            readable /= 1024;
            // Return formatted number with suffix
            return readable.ToString("0.## ") + suffix;
        }

        public void Notify()
        {
            OnPropertyChanged(nameof(CachedIconsSize));
            OnPropertyChanged(nameof(CachedIconCount));
        }
    }
}
