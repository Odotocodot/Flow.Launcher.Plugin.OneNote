using Flow.Launcher.Plugin.OneNote.UI;

namespace Flow.Launcher.Plugin.OneNote
{
	public enum IconTheme
	{
		System,
		Light,
		Dark,
		Color
	}
	
	public static class IconThemeExtensions
	{
		public static IconTheme ToIconTheme(this WindowsThemeWatcher.WindowsTheme theme)
		{
			return theme switch
			{
				WindowsThemeWatcher.WindowsTheme.Light => IconTheme.Dark,
				WindowsThemeWatcher.WindowsTheme.Dark => IconTheme.Light,
			};
		}
	}
}

