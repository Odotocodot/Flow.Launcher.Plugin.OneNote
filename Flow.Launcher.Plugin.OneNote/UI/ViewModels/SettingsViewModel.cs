
using System.Collections.Generic;
using System.IO;
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
        }

        public Settings Settings { get; init; }
        public KeywordViewModel[] Keywords { get; init; }
        public Icons Icons { get; init; }
        public KeywordViewModel SelectedKeyword { get; set; }

#pragma warning disable CA1822 // Mark members as static
        public IEnumerable<int> DefaultRecentCountOptions => Enumerable.Range(1, 16);
#pragma warning restore CA1822 // Mark members as static

        public string NotebookIcon => Path.Combine(context.CurrentPluginMetadata.PluginDirectory, Icons.Notebook);
        public string SectionIcon => Path.Combine(context.CurrentPluginMetadata.PluginDirectory, Icons.Section);
        public void OpenNotebookIconsFolder() => context.API.OpenDirectory(Icons.NotebookIconDirectory);
        public void OpenSectionIconsFolder() => context.API.OpenDirectory(Icons.SectionIconDirectory);
        public void ClearCachedIcons() => Icons.ClearCachedIcons();
    }
}