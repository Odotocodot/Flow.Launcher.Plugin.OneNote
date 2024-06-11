using System;
using System.Linq;
using Flow.Launcher.Plugin.OneNote.Icons;

namespace Flow.Launcher.Plugin.OneNote.UI.ViewModels
{
	public class IconThemeViewModel
	{
		private IconThemeViewModel(IconTheme iconTheme, PluginInitContext context)
		{
			IconTheme = iconTheme;
			string iconThemeString;
			if (iconTheme == IconTheme.System)
			{
				//TODO: Implement this
				iconThemeString = Enum.GetName(IconTheme.Light).ToLower();
				//ThemeManager.Current.ActualApplicationTheme
				Tooltip = "Match the system theme";
			}
			else
			{
				iconThemeString = Enum.GetName(iconTheme).ToLower();
			}
			ImageUri = new Uri(
				$"{context.CurrentPluginMetadata.PluginDirectory}/{IconConstants.ImagesDirectory}{IconConstants.Notebook}.{iconThemeString}.png");
		}

		public IconTheme IconTheme { get; }
		public Uri ImageUri { get; }
		public string Tooltip { get; }

		public static IconThemeViewModel[] GetIconThemeViewModels(PluginInitContext context) => 
			Enum.GetValues<IconTheme>().Select(iconTheme => new IconThemeViewModel(iconTheme, context)).ToArray();
	}

}