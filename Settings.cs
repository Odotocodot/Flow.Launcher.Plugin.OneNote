namespace Flow.Launcher.Plugin.OneNote
{
    public class Settings : UI.Model
    {
        private bool showUnread = true;
        private int defaultRecentsCount = 5;
        private bool showRecycleBin = true;
        private bool showEncrypted = false;
        private bool createColoredIcons = true;

        #region Seializing Keywords
        public Keyword NotebookExplorerKeyword { get; private set; } = new(0, "Notebook Explorer", $"nb:{Keyword.NotebookExplorerSeparator}", false);
        public Keyword RecentPagesKeyword { get; private set; } = new(1, "Recent Pages", "rcntpgs:", false);
        public Keyword TitleSearchKeyword { get; private set; } = new(2, "Search by title", "*", false, false);
        public Keyword ScopedSearchKeyword { get; private set; } = new(3, "Search in a scope", ">", false, false);
        #endregion

        public bool ShowRecycleBin
        {
            get => showRecycleBin;
            set => SetProperty(ref showRecycleBin, value);
        }
        public bool ShowUnread
        {
            get => showUnread;
            set => SetProperty(ref showUnread, value);
        }
        public int DefaultRecentsCount
        {
            get => defaultRecentsCount;
            set => SetProperty(ref defaultRecentsCount, value);
        }
        public bool ShowEncrypted 
        { 
            get => showEncrypted; 
            set => SetProperty(ref showEncrypted, value); 
        }
        public bool CreateColoredIcons
        {
            get => createColoredIcons;
            set => SetProperty(ref createColoredIcons, value);
        }
    }
}
