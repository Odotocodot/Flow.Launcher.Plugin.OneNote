using System.Linq;

namespace Flow.Launcher.Plugin.OneNote.UI
{
    public class ChangeKeywordViewModel : Model
    {
        private readonly SettingsViewModel settingsViewModel;
        private readonly PluginInitContext context;
        private string newKeyword;

        public ChangeKeywordViewModel(SettingsViewModel settingsViewModel)
        {
            context = settingsViewModel.context;
            this.settingsViewModel = settingsViewModel;
            SelectedKeyword = settingsViewModel.SelectedKeyword;
        }
        public string Tip => $"Enter the keyword you like to change \"{SelectedKeyword.Name}\" to.";
        public KeywordViewModel[] Keywords => settingsViewModel.Keywords;
        public KeywordViewModel SelectedKeyword { get; init; }
        public string NewKeyword { get => newKeyword; set => SetProperty(ref newKeyword, value); }

        public bool ChangeKeyword(out string errorMessage)
        {
            errorMessage = null;
            var oldKeyword = SelectedKeyword.Keyword;
            if (string.IsNullOrWhiteSpace(NewKeyword))
            {
                errorMessage = "The new keyword cannot be empty.";
                return false;
            }

            var newKeyword = NewKeyword.Trim();
            if (oldKeyword == newKeyword)
            {
                errorMessage = "The new keyword is the same as the old keyword.";
                return false;
            }

            var alreadySetKeyword = Keywords.FirstOrDefault(k => k.Keyword == newKeyword);
            if (alreadySetKeyword != null)
            {
                errorMessage = $"The new keyword matches an already set one:\n"
                                + $"\"{alreadySetKeyword.Name}\" => \"{alreadySetKeyword.Keyword}\"";
                return false;
            }

            SelectedKeyword.Keyword = newKeyword;
            settingsViewModel.UpdateSubtitleProperties();
            context.API.SaveSettingJsonStorage<Settings>();
            return true;
            
        }                                          
    }

}
