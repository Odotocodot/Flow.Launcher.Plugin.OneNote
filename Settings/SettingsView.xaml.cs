using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Flow.Launcher.Plugin.OneNote
{
    public partial class SettingsView : UserControl
    {
        private Settings Settings { get; set; }
        private ObservableCollection<int> Items { get; set; } = new ObservableCollection<int>(Enumerable.Range(1, 9));
        public SettingsView(Settings settings)
        {
            InitializeComponent();
            Settings = settings;
        }
    }
}
