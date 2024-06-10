using System;
using System.Linq;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.OneNote.UI.ViewModels
{
    public class ChangeKeywordViewModel : Model
    {
        private readonly PluginInitContext context;
        private readonly KeywordViewModel[] keywords;
        private readonly Action closeAction;

        private string errorMessage;

        public ChangeKeywordViewModel(SettingsViewModel settingsViewModel, PluginInitContext context, Action close)
        {
            this.context = context;
            closeAction = close;
            keywords = settingsViewModel.Keywords;
            SelectedKeyword = settingsViewModel.SelectedKeyword;
            ChangeKeywordCommand = new RelayCommand(
                keyword => ChangeKeyword((string)keyword),
                keyword => CanChangeKeyword((string)keyword));
            CloseCommand = new RelayCommand( _=> closeAction?.Invoke());
        }
        public KeywordViewModel SelectedKeyword { get; }

        public ICommand CloseCommand { get; }

        public ICommand ChangeKeywordCommand { get; }

        public string ErrorMessage
        {
            get => errorMessage;
            private set => SetProperty(ref errorMessage, value);
        }

        private bool CanChangeKeyword(string newKeyword)
        {
            if (string.IsNullOrWhiteSpace(newKeyword))
            {
                //ErrorMessage = "The new keyword cannot be empty.";
                return false;
            }

            newKeyword = newKeyword.Trim();
            if (SelectedKeyword.Keyword == newKeyword)
            {
                ErrorMessage = "The new keyword is the same as the old keyword.";
                return false;
            }

            var alreadySetKeyword = keywords.FirstOrDefault(k => k.Keyword == newKeyword);
            if (alreadySetKeyword != null)
            {
                ErrorMessage = $"The new keyword is already set for {alreadySetKeyword.Name}.";
                return false;
            }

            ErrorMessage = null;
            return true;
        }

        private void ChangeKeyword(string newKeyword)
        {
            SelectedKeyword.Keyword = newKeyword.Trim();
            context.API.SaveSettingJsonStorage<Settings>();
            closeAction?.Invoke();
        }
    }

}
