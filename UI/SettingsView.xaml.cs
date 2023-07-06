using ModernWpf.Controls;
using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.OneNote.UI
{
    public partial class SettingsView : UserControl
    {
        private readonly SettingsViewModel viewModel;
        public SettingsView(SettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = this.viewModel = viewModel;
        }

        private async void ClearCachedIcons(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog()
            {
                Title = "Clear Cached Icons",
                Content = $"Delete cached notebook and sections icons.\n" +
                          $"This will delete {viewModel.CachedIconCount} icon{(viewModel.CachedIconCount != 1 ? "s" : string.Empty)}.",
                PrimaryButtonText = "Yes",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
            };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
                viewModel.ClearCachedIcons();
        }

        //quick and dirty non MVVM stuffs
        private void OpenNotebookIconsFolder(object sender, RoutedEventArgs e)
        {
            SettingsViewModel.OpenNotebookIconsFolder();
        }

        private void OpenSectionIconsFolder(object sender, RoutedEventArgs e)
        {
            SettingsViewModel.OpenSectionIconsFolder();
        }

        private void StackPanel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            viewModel.NotifyGetOnlyProperties();

        }

        private void MenuFlyout_Closed(object sender, object e)
        {
            viewModel.ClosedFlyout();
        }

        private void MenuFlyout_Opened(object sender, object e)
        {
            viewModel.OpenedFlyout();
        }
    }
}
