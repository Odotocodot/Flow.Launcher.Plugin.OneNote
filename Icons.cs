using Odotocodot.OneNote.Linq;
using System.IO;

namespace Flow.Launcher.Plugin.OneNote
{
    public static class Icons
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

        private static OneNoteItemIcons notebookIcons;
        private static OneNoteItemIcons sectionIcons;
        private static Settings settings;
        
        public static int CachedIconCount => notebookIcons.CachedIconCount + sectionIcons.CachedIconCount;
        public static string NotebookIconDirectory { get; private set; }
        public static string SectionIconDirectory {get; private set; }

        public static void Init(PluginInitContext context, Settings settings)
        {
            NotebookIconDirectory = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, "Images", "NotebookIcons");
            SectionIconDirectory = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, "Images", "SectionIcons");

            notebookIcons = new OneNoteItemIcons(NotebookIconDirectory, Path.Combine(context.CurrentPluginMetadata.PluginDirectory, Notebook));
            sectionIcons = new OneNoteItemIcons(SectionIconDirectory, Path.Combine(context.CurrentPluginMetadata.PluginDirectory, Section));

            Icons.settings = settings;
        }
        public static string GetIcon(IOneNoteItem item)
        {
            return item switch
            {
                OneNoteNotebook notebook => settings.CreateColoredIcons && notebook.Color.HasValue
                                                ? notebookIcons.GetIcon(notebook.Color.Value)
                                                : Notebook,
                OneNoteSectionGroup sectionGroup => sectionGroup.IsRecycleBin
                                                        ? RecycleBin
                                                        : SectionGroup,
                OneNoteSection section => settings.CreateColoredIcons && section.Color.HasValue
                                              ? sectionIcons.GetIcon(section.Color.Value)
                                              : Section,
                OneNotePage => Page,
                _ => Warning,
            };
        }

        public static long GetIconsFileSize() => notebookIcons.GetIconsFileSize() + sectionIcons.GetIconsFileSize();

        public static void ClearCachedIcons() 
        {
            notebookIcons.ClearCachedIcons();
            sectionIcons.ClearCachedIcons();
        } 

    }
}