using System.Windows.Controls;
using Flow.Launcher.Plugin.OneNote.Icons;
using Flow.Launcher.Plugin.OneNote.UI.ViewModels;

namespace Flow.Launcher.Plugin.OneNote.UI.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView(PluginInitContext context, Settings settings, IconProvider iconProvider)
        {
            InitializeComponent();
            DataContext = new SettingsViewModel(context, settings, iconProvider);
        }
    }
}
