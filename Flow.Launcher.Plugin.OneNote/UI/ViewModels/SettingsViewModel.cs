using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Flow.Launcher.Plugin.OneNote.Icons;
using Modern = ModernWpf.Controls;

namespace Flow.Launcher.Plugin.OneNote.UI.ViewModels
{
    public class SettingsViewModel : Model
    {
        private readonly IconProvider iconProvider;
        private KeywordViewModel selectedKeyword;

        public SettingsViewModel(PluginInitContext context, Settings settings, IconProvider iconProvider)
        {
            this.iconProvider = iconProvider;
            Settings = settings;
            Keywords = KeywordViewModel.GetKeywordViewModels(settings.Keywords); 
            IconThemes = IconThemeViewModel.GetIconThemeViewModels(context);
            
            EditCommand = new RelayCommand(
	            _ => new Views.ChangeKeywordWindow(this, context).ShowDialog(), //Avert your eyes! This is not MVVM!
	            _ => SelectedKeyword != null);

            OpenGeneratedIconsFolderCommand = new RelayCommand(
	            _ => context.API.OpenDirectory(iconProvider.GeneratedImagesDirectoryInfo.FullName));
            
            ClearCachedIconsCommand = new RelayCommand(
	            async _ => await ClearCachedIcons(),
				_ => iconProvider.CachedIconCount > 0);
            
            iconProvider.PropertyChanged += (_, args) =>
            {
	            if (args.PropertyName == nameof(iconProvider.CachedIconCount))
	            {
		            OnPropertyChanged(nameof(CachedIconsFileSize));
		            CommandManager.InvalidateRequerySuggested();
	            }
            };
            SelectedKeyword = Keywords[0];
        }
        public ICommand EditCommand { get; }
        public ICommand OpenGeneratedIconsFolderCommand { get; }
        public ICommand ClearCachedIconsCommand { get; }
        public Settings Settings { get; }
        public KeywordViewModel[] Keywords { get; }
        public IconThemeViewModel[] IconThemes { get; }
        public string CachedIconsFileSize => iconProvider.GetCachedIconsMemorySize();

        public KeywordViewModel SelectedKeyword
        {
	        get => selectedKeyword;
	        set => SetProperty(ref selectedKeyword, value);
        }
        
        //quick and dirty non MVVM stuffs
        private async Task ClearCachedIcons()
        {
	        var dialog = new Modern.ContentDialog()
	        {
		        Title = "Clear Cached Icons",
		        Content = $"Delete cached notebook and sections icons.\n" +
		                  $"This will delete {iconProvider.CachedIconCount} icon{(iconProvider.CachedIconCount != 1 ? "s" : string.Empty)}.",
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