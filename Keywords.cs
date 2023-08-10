namespace Flow.Launcher.Plugin.OneNote
{
    public class Keywords : UI.Model
    {
        public const string NotebookExplorerSeparator = "\\";
        private string recentPages;
        private string notebookExplorer = $"nb:{NotebookExplorerSeparator}";
        private string titleSearch = "*";
        private string scopedSearch = ">";

        public string NotebookExplorer { get => notebookExplorer; set => SetProperty(ref notebookExplorer, value); }
        public string RecentPages { get => recentPages ?? "rcntpgs:"; set => SetProperty(ref recentPages, value); }
        public string TitleSearch { get => titleSearch; set => SetProperty(ref titleSearch, value); }
        public string ScopedSearch { get => scopedSearch; set => SetProperty(ref scopedSearch, value); }
    }
}