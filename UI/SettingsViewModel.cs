using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Flow.Launcher.Plugin.OneNote.UI
{
    public class SettingsViewModel : Model
    {
        public readonly PluginInitContext context;
        public SettingsViewModel(PluginInitContext context, Settings settings)
        {
            Settings = settings;
            this.context = context;
            NotebookIcon = Directory.EnumerateFiles(Icons.NotebookIconDirectory).FirstOrDefault(Path.Combine(context.CurrentPluginMetadata.PluginDirectory, Icons.Notebook)); 
            SectionIcon = Directory.EnumerateFiles(Icons.SectionIconDirectory).FirstOrDefault(Path.Combine(context.CurrentPluginMetadata.PluginDirectory, Icons.Section));
            Keywords = KeywordViewModel.GetKeywordViewModels(settings.Keywords);
        }

        public Settings Settings { get; init; }
        public KeywordViewModel[] Keywords { get; init; }

        public KeywordViewModel SelectedKeyword { get; set; }

        public string RecycleBinSubTitle => $"When using \"{Keywords[0].Keyword}\" show items that are in the recycle bin";
        public string EncryptedSectionSubTitle => $"when using \"{Keywords[0].Keyword}\" show encrypted sections, if the section has been unlocked, allow temporary access." ;
        public string RecentPagesSubTitle => $"The initial number of recent pages to show when using \"{Keywords[1].Keyword}\"";

#pragma warning disable CA1822 // Mark members as static
        public IEnumerable<int> DefaultRecentCountOptions => Enumerable.Range(1, 16);
        public int CachedIconCount => Icons.CachedIconCount;
        public string CachedIconsSize => GetBytesReadable(Icons.GetIconsFileSize());
        public bool EnableClearIconButton => Icons.CachedIconCount > 0;
#pragma warning restore CA1822 // Mark members as static

        public string NotebookIcon { get; init; }
        public string SectionIcon { get; init; }
        public void OpenNotebookIconsFolder() => context.API.OpenDirectory(Icons.NotebookIconDirectory);//  Process.Start(new ProcessStartInfo { FileName = $"\"{Icons.NotebookIconDirectory}\"", UseShellExecute = true });
        public void OpenSectionIconsFolder() => context.API.OpenDirectory(Icons.SectionIconDirectory);
        public void ClearCachedIcons()
        {
            Icons.ClearCachedIcons();
            UpdateIconProperties();
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

        public void UpdateIconProperties()
        {
            OnPropertyChanged(nameof(CachedIconsSize));
            OnPropertyChanged(nameof(CachedIconCount));
            OnPropertyChanged(nameof(EnableClearIconButton));
        }

        public void UpdateSubtitleProperties()
        {
            OnPropertyChanged(nameof(RecycleBinSubTitle));
            OnPropertyChanged(nameof(EncryptedSectionSubTitle));
            OnPropertyChanged(nameof(RecentPagesSubTitle));
        }
    }
}