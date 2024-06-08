
using System;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.OneNote.UI.ViewModels
{
    public class SettingsViewModel : Model
    {
        public readonly PluginInitContext context;
        public SettingsViewModel(PluginInitContext context, Settings settings, Icons iconProvider)
        {
            Settings = settings;
            this.context = context;
            Keywords = KeywordViewModel.GetKeywordViewModels(settings.Keywords);
            IconProvider = iconProvider;
            IconProvider.PropertyChanged += (sender, args) => OnPropertyChanged(nameof(CanClearCachedIcons));
        }

        public Settings Settings { get; init; }
        public KeywordViewModel[] Keywords { get; init; }
        //TODO refactor this expose the properties instead of the class
        public Icons IconProvider { get; init; }
        public KeywordViewModel SelectedKeyword { get; set; }

#pragma warning disable CA1822 // Mark members as static
        public IEnumerable<int> DefaultRecentCountOptions => Enumerable.Range(1, 16);
        public IEnumerable<IconTheme> PluginThemes => Enum.GetValues<IconTheme>();
#pragma warning restore CA1822 // Mark members as static
        public void OpenGeneratedIconsFolder() => context.API.OpenDirectory(IconProvider.GeneratedImagesDirectoryInfo.FullName);
        public void ClearCachedIcons() => IconProvider.ClearCachedIcons();
        public bool CanClearCachedIcons => IconProvider.CachedIconCount > 0;
    }
}