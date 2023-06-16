﻿namespace Flow.Launcher.Plugin.OneNote
{
    public class Settings : BaseModel
    {
        private bool showUnread = true;
        private int defaultRecentsCount = 5;
        private bool showRecycleBin = true;
        private bool showEncrypted = false;

        public bool ShowRecycleBin
        {
            get=> showRecycleBin;
            set
            {
                showRecycleBin = value;
                OnPropertyChanged();
            }
        }
        public bool ShowUnread
        {
            get => showUnread;
            set 
            { 
                showUnread = value; 
                OnPropertyChanged();
            }
        }
        public int DefaultRecentsCount
        {
            get => defaultRecentsCount;
            set
            {
                defaultRecentsCount = value;
				OnPropertyChanged();
            }
        }

        public bool ShowEncrypted 
        { 
            get => showEncrypted; 
            set 
            { 
                showEncrypted = value;
                OnPropertyChanged();
            } 
        }

    }
}
