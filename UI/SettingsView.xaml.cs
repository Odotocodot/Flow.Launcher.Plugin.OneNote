using System.Windows.Controls;

namespace Flow.Launcher.Plugin.OneNote.UI
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
