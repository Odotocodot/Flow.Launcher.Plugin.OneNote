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
			if (iconTheme == IconTheme.System)
			{
				Name = "FL Default";
				ImageUri = GetUri(IconTheme.Light.ToString(), context);
				ImageUri2 = GetUri(IconTheme.Dark.ToString(), context);
				Tooltip = "Matches Flow Launcher's app theme";
			}
			else
			{
				Name = Enum.GetName(iconTheme);
				ImageUri = GetUri(Name, context);
			}
		}

		private static Uri GetUri(string theme, PluginInitContext context) =>
			new($"{context.CurrentPluginMetadata.PluginDirectory}/{IconConstants.ImagesDirectory}{IconConstants.Notebook}.{theme.ToLower()}.png");

		public string Name { get; }
		public IconTheme IconTheme { get; }
		public Uri ImageUri { get; }
		public Uri ImageUri2 { get; }
		public string Tooltip { get; }
		
		public static IconThemeViewModel[] GetIconThemeViewModels(PluginInitContext context) => 
			Enum.GetValues<IconTheme>().Select(iconTheme => new IconThemeViewModel(iconTheme, context)).ToArray();
	}

}