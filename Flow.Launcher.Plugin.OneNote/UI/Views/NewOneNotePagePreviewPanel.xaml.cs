using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Flow.Launcher.Plugin.OneNote.UI.ViewModels;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote.UI.Views
{
	public partial class NewOneNotePagePreviewPanel : UserControl
	{
		public NewOneNotePagePreviewPanel(PluginInitContext context, OneNoteSection section, string pageTitle)
		{
			DataContext = new NewOneNotePageViewModel(context, section, pageTitle);
			InitializeComponent();
		}
		private void NewOneNotePagePreviewPanel_OnLoaded(object sender, RoutedEventArgs e)
		{
			FocusTextBox();
        }
	
		private void NewOneNotePagePreviewPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if((bool)e.NewValue)
            {
                FocusTextBox();
            }
        }

        private void FocusTextBox()
        {
            TextBoxPageTitle.Focus();
            Keyboard.Focus(TextBoxPageTitle);
            TextBoxPageTitle.CaretIndex = TextBoxPageTitle.Text.Length;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Tab) 
				return;
		
			if (e.Source is not (TextBox or Button)) 
				return;
		
			var focusNavigationDirection = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
				? FocusNavigationDirection.Previous
				: FocusNavigationDirection.Next;
			((UIElement)Keyboard.FocusedElement)?.MoveFocus(new TraversalRequest(focusNavigationDirection));
		
			e.Handled = true;
		}
	}
}