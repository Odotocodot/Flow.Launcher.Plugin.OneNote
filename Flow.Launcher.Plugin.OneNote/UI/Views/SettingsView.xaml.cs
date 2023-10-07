using Modern = ModernWpf.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Flow.Launcher.Plugin.OneNote.UI.ViewModels;

namespace Flow.Launcher.Plugin.OneNote.UI.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly SettingsViewModel viewModel;
        public SettingsView(SettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = this.viewModel = viewModel;
        }

        //quick and dirty non MVVM stuffs
        private async void ClearCachedIcons(object sender, RoutedEventArgs e)
        {
            var input = (UIElement)sender;
            var temp = input.IsEnabled;
            input.IsEnabled = false;

            var dialog = new Modern.ContentDialog()
            {
                Title = "Clear Cached Icons",
                Content = $"Delete cached notebook and sections icons.\n" +
                          $"This will delete {viewModel.Icons.CachedIconCount} icon{(viewModel.Icons.CachedIconCount != 1 ? "s" : string.Empty)}.",
                PrimaryButtonText = "Yes",
                CloseButtonText = "Cancel",
                DefaultButton = Modern.ContentDialogButton.Close,
            };

            var result = await dialog.ShowAsync();

            if (result == Modern.ContentDialogResult.Primary)
            {
                viewModel.ClearCachedIcons();
                input.IsEnabled = false;
            }
            else
            {
                input.IsEnabled = temp;
            }
        }

        private void OpenNotebookIconsFolder(object sender, RoutedEventArgs e)
        {
            viewModel.OpenNotebookIconsFolder();
        }

        private void OpenSectionIconsFolder(object sender, RoutedEventArgs e)
        {
            viewModel.OpenSectionIconsFolder();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.SelectedKeyword == null)
            {
                MessageBox.Show("Please select a keyword");
            }
            else
            {
                new ChangeKeywordWindow(viewModel).ShowDialog();
            }
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var listView = (ListView)sender;

                var hit = listView.InputHitTest(e.GetPosition(listView));
                if (hit is FrameworkElement fe && fe.DataContext is KeywordViewModel selectedKeyword)
                {
                    listView.SelectedItem = selectedKeyword;
                    EditButton_Click(sender, e);
                }
            }
        }

        private void ListView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                var listView = (ListView)sender;

                var hit = listView.InputHitTest(e.GetPosition(listView));
                if (hit is FrameworkElement fe && fe.DataContext is KeywordViewModel selectKeyword)
                {
                    listView.SelectedItem = selectKeyword;

                    var menuItem = new MenuItem();
                    menuItem.Click += EditButton_Click;
                    menuItem.Header = "Edit";
                    var contextMenu = new ContextMenu();
                    contextMenu.Items.Add(menuItem);
                    contextMenu.IsOpen = true;
                }
            }
        }
    }
}
