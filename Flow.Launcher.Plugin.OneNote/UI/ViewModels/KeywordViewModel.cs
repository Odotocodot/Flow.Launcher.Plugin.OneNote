namespace Flow.Launcher.Plugin.OneNote.UI.ViewModels
{
    public class KeywordViewModel : Model
    {
        private readonly Keyword keyword;
        public KeywordViewModel(string keywordName, Keyword keyword)
        {
            Name = keywordName;
            this.keyword = keyword;
        }
        public string Name { get; }
        
        public string Value
        {
            get => keyword.Value;
            set
            {
                keyword.ChangeKeyword(value);
                OnPropertyChanged();
            }
        }
    }
}
