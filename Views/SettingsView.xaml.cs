using Flow.Launcher.Plugin.OneNote.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly Regex regex = new Regex("[^0-9.-]+");
        public SettingsView(Settings settings)
        {
            InitializeComponent();
            DataContext = new SettingsViewModel(settings);
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
