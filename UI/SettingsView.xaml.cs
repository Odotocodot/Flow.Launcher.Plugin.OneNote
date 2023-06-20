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

        public void ClearCachedIcons()
        {
            var result = MessageBox.Show("Delete cached icons for notebooks and sections.", "Clear Cached Icons", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                viewModel.ClearCachedIcons();
            }
        }
    }
}
