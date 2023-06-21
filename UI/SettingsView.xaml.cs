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
            LostFocus += (s,e) => viewModel.Notify();
            Unloaded += (s,e) => viewModel.Notify();
        }

        public async void ClearCachedIcons(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "Clear Cached Icons",
                Content = $"Delete cached notebook and sections icons.\n" +
                          $"This will delete {viewModel.CachedIconCount} icon{(viewModel.CachedIconCount != 1 ? "s" : string.Empty)}.",
                PrimaryButtonText = "Yes",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
            };
            var result = await dialog.ShowAsync();
            if(result == ContentDialogResult.Primary)
            {
                viewModel.ClearCachedIcons();
            }
        }
    }
}
