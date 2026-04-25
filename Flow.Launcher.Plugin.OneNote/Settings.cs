using Flow.Launcher.Plugin.OneNote.Icons;

namespace Flow.Launcher.Plugin.OneNote
{
	public class Settings : UI.Model
	{
		private bool showUnread = true;
		private bool showRecycleBin = true;
		private bool showEncrypted;
		private bool createColoredIcons = true;
		private int maxResults = 30;
		private bool useMaxResults = true;
		private bool alwaysOpenInNewWindow = false;
		private IconTheme iconTheme = IconTheme.Color;
		public Keywords Keywords { get; init; } = new Keywords();

		public bool ShowRecycleBin
		{
			get => showRecycleBin;
			set => SetProperty(ref showRecycleBin, value);
		}

		public bool ShowUnread
		{
			get => showUnread;
			set => SetProperty(ref showUnread, value);
		}

		public int MaxResults
		{
			get => maxResults;
			set => SetProperty(ref maxResults, value);
		}

		public bool UseMaxResults
		{
			get => useMaxResults;
			set => SetProperty(ref useMaxResults, value);
		}

		public bool ShowEncrypted
		{
			get => showEncrypted;
			set => SetProperty(ref showEncrypted, value);
		}

		public bool CreateColoredIcons
		{
			get => createColoredIcons;
			set => SetProperty(ref createColoredIcons, value);
		}

		public IconTheme IconTheme
		{
			get => iconTheme;
			set => SetProperty(ref iconTheme, value);
		}

		public bool AlwaysOpenInNewWindow
		{
			get => alwaysOpenInNewWindow;
			set => alwaysOpenInNewWindow = value;
		}
	}
}