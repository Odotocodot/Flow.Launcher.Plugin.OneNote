using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.OneNote
{
    public class Settings : BaseModel
    {
		private bool fastMode;
		private bool showUnread;
        private bool showRecycleBin;
		private int defaultRecentsCount;

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

        public bool ShowRecycleBin
        {
            get => showRecycleBin;
            set 
            { 
                showRecycleBin = value;
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


	}
}
