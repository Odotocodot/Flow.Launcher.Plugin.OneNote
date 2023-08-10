using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.OneNote
{
    public class Settings : UI.Model
    {
        private bool showUnread = true;
        private int defaultRecentsCount = 5;
        private bool showRecycleBin = true;
        private bool showEncrypted = false;
        private bool createColoredIcons = true;
  

        [JsonIgnore]
        public Keywords Keywords { get ; private set ; } = new Keywords();

        #region For Saving Keywords

        [JsonInclude]
        private string RecentPagesKeyword { get; set }

        #endregion
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
    }
}
