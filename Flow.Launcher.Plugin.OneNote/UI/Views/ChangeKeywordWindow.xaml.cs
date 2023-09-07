using System.Windows;
using Flow.Launcher.Plugin.OneNote.UI.ViewModels;

namespace Flow.Launcher.Plugin.OneNote.UI.Views
{
    public partial class ChangeKeywordWindow
    {
        private readonly ChangeKeywordViewModel viewModel;

        public ChangeKeywordWindow(SettingsViewModel settingsViewModel)
        {
            InitializeComponent();
            DataContext = viewModel = new ChangeKeywordViewModel(settingsViewModel);
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            TextBox_NewKeyword.Focus();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_ChangeKeyword(object sender, RoutedEventArgs e)
        {
            if(viewModel.ChangeKeyword(out string errorMessage))
            {
                Close();
            }
            else
            {
                MessageBox.Show(this, errorMessage,"Invalid Keyword", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
