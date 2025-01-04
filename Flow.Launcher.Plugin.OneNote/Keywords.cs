namespace Flow.Launcher.Plugin.OneNote
{
    public class Keywords
    {
        public const string NotebookExplorerSeparator = "\\";
        public string NotebookExplorer { get; set; } = $"nb:{NotebookExplorerSeparator}";
        public string RecentPages { get; set; } = "rp:";
        public string TitleSearch { get; set; } = "*";
        public string ScopedSearch { get; set; } = ">";
    }
}