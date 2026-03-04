using System;
using Flow.Launcher.Plugin.OneNote.Icons;

namespace Flow.Launcher.Plugin.OneNote.UI.ViewModels
{
	public class IconThemeViewModel
	{
		public IconThemeViewModel(IconTheme iconTheme, PluginInitContext context)
		{
			IconTheme = iconTheme;
			if (iconTheme == IconTheme.System)
			{
				Name = "FL Default";
				ImageUri = GetUri(nameof(IconTheme.Light), context);
				ImageUri2 = GetUri(nameof(IconTheme.Dark), context);
				Tooltip = "Matches Flow Launcher's app theme";
			}
			else
			{
				Name = iconTheme.ToString();
				ImageUri = GetUri(Name, context);
			}
		}

		private static Uri GetUri(string theme, PluginInitContext context) =>
			new($"{context.CurrentPluginMetadata.PluginDirectory}/{IconConstants.ImagesDirectory}{IconConstants.Notebook}.{theme.ToLower()}.png");

		public string Name { get; }
		public IconTheme IconTheme { get; }
		public Uri ImageUri { get; }
		public Uri? ImageUri2 { get; }
		public string? Tooltip { get; }
	}
}