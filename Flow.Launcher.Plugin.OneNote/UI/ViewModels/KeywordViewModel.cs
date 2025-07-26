namespace Flow.Launcher.Plugin.OneNote.UI.ViewModels
{
    public class KeywordViewModel : BaseModel
    {
        public KeywordViewModel(string keywordName, Keyword keyword)
        {
            Name = keywordName;
            Keyword = keyword;
            keyword.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(Keyword.Value))
                {
                    OnPropertyChanged(nameof(Keyword));
                }
            };
        }
        public string Name { get; }
        public Keyword Keyword { get; }
    }
}
