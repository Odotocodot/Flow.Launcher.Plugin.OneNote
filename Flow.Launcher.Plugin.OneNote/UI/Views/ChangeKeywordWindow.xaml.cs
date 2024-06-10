using System.Windows;
using Flow.Launcher.Plugin.OneNote.UI.ViewModels;

namespace Flow.Launcher.Plugin.OneNote.UI.Views
{
    public partial class ChangeKeywordWindow
    {
        public ChangeKeywordWindow(SettingsViewModel viewModel, PluginInitContext context)
        {
            InitializeComponent();
            DataContext = new ChangeKeywordViewModel(viewModel, context, Close);
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            TextBox_NewKeyword.Focus();
        }
    }
}
