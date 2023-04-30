using Flow.Launcher.Plugin.OneNote.ViewModels;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.OneNote
{
    public partial class SettingsView : UserControl
    {
        public SettingsView(Settings settings)
        {
            InitializeComponent();
            DataContext = new SettingsViewModel(settings);
        }
    }
}
