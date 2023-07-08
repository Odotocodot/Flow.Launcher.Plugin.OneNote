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
        public Keyword NotebookExplorerKeyword { get; private set; } = new("Notebook Explorer", $"nb:{Keyword.NotebookExplorerSeparator}");
        public Keyword RecentPagesKeyword { get; private set; } = new("Recent Pages", "rcntpgs:");
        public Keyword TitleSearchKeyword { get; private set; } = new("Title Search", "*");
        public Keyword ScopedSearchKeyword { get; private set; } = new("Scoped Search", ">");
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
