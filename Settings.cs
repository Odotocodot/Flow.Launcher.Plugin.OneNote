﻿using Flow.Launcher.Plugin.OneNote.ViewModels;

namespace Flow.Launcher.Plugin.OneNote
{
    public class Settings : BaseModel
    {
		private bool fastMode;
        private bool showUnread = true;
        private int defaultRecentsCount = 5;
        private int comReleaseTimeout = 10;
        private TimeType timeType = TimeType.milliseconds;


        public bool FastMode
		{
			get => fastMode; 
            set
            {
                fastMode = value;
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
        public int COMReleaseTimeout
        {
            get => comReleaseTimeout;
            set 
            { 
                comReleaseTimeout = value;
                OnPropertyChanged();
            }
        }
        public TimeType TimeType
        {
            get => timeType;
            set 
            {
                timeType = value;
                OnPropertyChanged();
            }
        }
	}
}
