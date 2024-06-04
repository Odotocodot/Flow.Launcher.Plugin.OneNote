
using System;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.OneNote.UI.ViewModels
{
    public class SettingsViewModel : Model
    {
        public readonly PluginInitContext context;
        public SettingsViewModel(PluginInitContext context, Settings settings)
        {
            Settings = settings;
            this.context = context;
            Keywords = KeywordViewModel.GetKeywordViewModels(settings.Keywords);
            Icons = Icons.Instance;
            Icons.PropertyChanged += (sender, args) => OnPropertyChanged(nameof(CanClearCachedIcons));
        }

        public Settings Settings { get; init; }
        public KeywordViewModel[] Keywords { get; init; }
        public Icons Icons { get; init; }
        public KeywordViewModel SelectedKeyword { get; set; }

#pragma warning disable CA1822 // Mark members as static
        public IEnumerable<int> DefaultRecentCountOptions => Enumerable.Range(1, 16);
        public IEnumerable<IconTheme> PluginThemes => Enum.GetValues<IconTheme>();
#pragma warning restore CA1822 // Mark members as static
        public void OpenGeneratedIconsFolder() => context.API.OpenDirectory(Icons.GeneratedImagesDirectoryInfo.FullName);
        public void ClearCachedIcons() => Icons.ClearCachedIcons();
        public bool CanClearCachedIcons => Icons.CachedIconCount > 0;
    }
}