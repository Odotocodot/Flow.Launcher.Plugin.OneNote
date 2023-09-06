using Odotocodot.OneNote.Linq;
using System;
using System.IO;

namespace Flow.Launcher.Plugin.OneNote
{
    public class Icons : BaseModel
    {
        public const string Logo = "Images/logo.png";
        public const string Unavailable = "Images/unavailable.png";
        public const string Sync = "Images/refresh.png";
        public const string Warning = "Images/warning.png";
        public const string Search = Logo;
        public const string RecycleBin = "Images/recycle_bin.png";
        public const string Recent = "Images/recent.png";
        public const string RecentPage = "Images/recent_page.png";

        public const string Page = Logo;
        public const string Section = "Images/section.png";
        public const string SectionGroup = "Images/section_group.png";
        public const string Notebook = "Images/notebook.png";
        
        public const string NewPage = "Images/new_page.png";
        public const string NewSection = "Images/new_section.png";
        public const string NewSectionGroup = "Images/new_section_group.png";
        public const string NewNotebook = "Images/new_notebook.png";

        private OneNoteItemIcons notebookIcons;
        private OneNoteItemIcons sectionIcons;
        private Settings settings;

        public int CachedIconCount => notebookIcons.IconCount + sectionIcons.IconCount;
        public string CachedIconsFileSize => GetBytesReadable(notebookIcons.IconsFileSize + sectionIcons.IconsFileSize);
        public static string NotebookIconDirectory { get; private set; }
        public static string SectionIconDirectory { get; private set; }


        private static readonly Lazy<Icons> lazy = new();
        public static Icons Instance => lazy.Value;

        public static void Init(PluginInitContext context, Settings settings)
        {
            NotebookIconDirectory = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, "Images", "NotebookIcons");
            SectionIconDirectory = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, "Images", "SectionIcons");

            Instance.notebookIcons = new OneNoteItemIcons(NotebookIconDirectory, Path.Combine(context.CurrentPluginMetadata.PluginDirectory, Notebook));
            Instance.sectionIcons = new OneNoteItemIcons(SectionIconDirectory, Path.Combine(context.CurrentPluginMetadata.PluginDirectory, Section));


            Instance.notebookIcons.PropertyChanged += Instance.IconCountChanged;
            Instance.sectionIcons.PropertyChanged += Instance.IconCountChanged;

            Instance.settings = settings;
        }

        public static void Close()
        {
            Instance.notebookIcons.PropertyChanged -= Instance.IconCountChanged;
            Instance.sectionIcons.PropertyChanged -= Instance.IconCountChanged;
        }
        private void IconCountChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(CachedIconCount));
            OnPropertyChanged(nameof(CachedIconsFileSize));
        }

        public static string GetIcon(IOneNoteItem item)
        {
            return item switch
            {
                OneNoteNotebook notebook => Instance.settings.CreateColoredIcons && notebook.Color.HasValue
                                                ? Instance.notebookIcons.GetIcon(notebook.Color.Value)
                                                : Notebook,
                OneNoteSectionGroup sectionGroup => sectionGroup.IsRecycleBin
                                                        ? RecycleBin
                                                        : SectionGroup,
                OneNoteSection section => Instance.settings.CreateColoredIcons && section.Color.HasValue
                                              ? Instance.sectionIcons.GetIcon(section.Color.Value)
                                              : Section,
                OneNotePage => Page,
                _ => Warning,
            };
        }

        public void ClearCachedIcons() 
        {
            notebookIcons.ClearCachedIcons();
            sectionIcons.ClearCachedIcons();
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

    }
}