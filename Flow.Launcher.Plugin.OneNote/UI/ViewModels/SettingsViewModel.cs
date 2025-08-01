using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Flow.Launcher.Plugin.OneNote.Icons;
using Humanizer;
using Modern = ModernWpf.Controls;

namespace Flow.Launcher.Plugin.OneNote.UI.ViewModels
{
    public class SettingsViewModel : Model
    {
        private readonly IconProvider iconProvider;
        private KeywordViewModel? selectedKeyword;

        public SettingsViewModel(PluginInitContext context, Settings settings, IconProvider iconProvider)
        {
            this.iconProvider = iconProvider;
            Settings = settings;
            Keywords = settings.Keywords //Order is the order they are written in Keywords.cs
                               .GetType()
                               .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                               .Select(p => new KeywordViewModel(p.Name.Humanize(LetterCasing.Title), (Keyword)p.GetValue(settings.Keywords)!))
                               .ToArray();
            IconThemes = Enum.GetValues<IconTheme>()
                             .Select(iconTheme => new IconThemeViewModel(iconTheme, context))
                             .ToArray();
            
            EditCommand = new RelayCommand(
	            () => new Views.ChangeKeywordWindow(this, context).ShowDialog(), //Avert your eyes! This is not MVVM!
	            () => SelectedKeyword != null);

            OpenGeneratedIconsFolderCommand = new RelayCommand(
	            () => context.API.OpenDirectory(iconProvider.GeneratedImagesDirectoryInfo.FullName));
            
            ClearCachedIconsCommand = new RelayCommand(
	            async () => await ClearCachedIcons(),
				() => iconProvider.CachedIconCount > 0);
            
            iconProvider.PropertyChanged += (_, args) =>
            {
	            if (args.PropertyName == nameof(iconProvider.CachedIconCount))
	            {
		            OnPropertyChanged(nameof(CachedIconsFileSize));
		            CommandManager.InvalidateRequerySuggested();
	            }
            };
            settings.PropertyChanged += (_, args) =>
			{
	            if (args.PropertyName == nameof(Settings.IconTheme))
	            {
		            context.API.ReQuery();
	            }
			};
            SelectedKeyword = Keywords[0];
        }
        public ICommand EditCommand { get; }
        public ICommand OpenGeneratedIconsFolderCommand { get; }
        public ICommand ClearCachedIconsCommand { get; }
        public Settings Settings { get; }
        public KeywordViewModel[] Keywords { get; }
        public KeywordViewModel NotebookExplorerKeyword => Keywords[0];
        public KeywordViewModel RecentPagesKeyword => Keywords[1];
        public IconThemeViewModel[] IconThemes { get; }
        public string CachedIconsFileSize => iconProvider.GeneratedImagesDirectoryInfo.EnumerateFiles()
			.Select(file => file.Length)
	        .Aggregate(0L, (a, b) => a + b)
			.Bytes()
			.Humanize();

        public KeywordViewModel? SelectedKeyword
        {
	        get => selectedKeyword;
	        set => SetProperty(ref selectedKeyword, value);
        }
        
        //quick and dirty non MVVM stuffs
        private async Task ClearCachedIcons()
        {
	        var dialog = new Modern.ContentDialog
	        {
		        Title = "Clear Cached Icons",
		        Content = $"Delete cached notebook and sections icons.\n" +
		                  $"This will delete {"icon".ToQuantity(iconProvider.CachedIconCount)}.",
		        PrimaryButtonText = "Yes",
		        CloseButtonText = "Cancel",
		        DefaultButton = Modern.ContentDialogButton.Close,
	        };

	        var result = await dialog.ShowAsync();

	        if (result == Modern.ContentDialogResult.Primary)
	        {
		        iconProvider.ClearCachedIcons();
	        }
        }
    }
}