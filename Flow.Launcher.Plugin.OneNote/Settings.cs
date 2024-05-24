namespace Flow.Launcher.Plugin.OneNote
{
    public class Settings : UI.Model
    {
        private bool showUnread = true;
        private int defaultRecentsCount = 5;
        private bool showRecycleBin = true;
        private bool showEncrypted = false;
        private bool createColoredIcons = true;
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
        public int DefaultRecentsCount
        {
            get => defaultRecentsCount;
            set => SetProperty(ref defaultRecentsCount, value);
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

        public PluginTheme PluginTheme { get; set; } = PluginTheme.Color;
    }
}
