using System;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.OneNote.ViewModels
{
    public class SettingsViewModel
    {
        public SettingsViewModel(Settings settings)
        {
            Settings = settings;
        }
        public Settings Settings { get; init; }
        public IEnumerable<int> DefaultRecentCountOptions => Enumerable.Range(1, 16);

        public IEnumerable<TimeType> TimeTypes => Enum.GetValues<TimeType>().Cast<TimeType>();

        //Bind Small Change
    }

    public enum TimeType
    {
        milliseconds,
        seconds,
        minutes,
        Infinite
    }
}
