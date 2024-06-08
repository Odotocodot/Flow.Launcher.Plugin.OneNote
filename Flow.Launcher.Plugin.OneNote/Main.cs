using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Odotocodot.OneNote.Linq;
namespace Flow.Launcher.Plugin.OneNote
{
    public class Main : IAsyncPlugin, IContextMenu, ISettingProvider, IDisposable
    {
        private PluginInitContext context;

        private ResultCreator resultCreator;
        private SearchManager searchManager;
        private Settings settings;
        private IconProvider iconProvider;

        private static SemaphoreSlim semaphore;
        public Task InitAsync(PluginInitContext context)
        {
            this.context = context;
            settings = context.API.LoadSettingJsonStorage<Settings>();
            iconProvider = new IconProvider(context, settings);
            resultCreator = new ResultCreator(context, settings, iconProvider);
            searchManager = new SearchManager(context, settings, resultCreator);
            semaphore = new SemaphoreSlim(1,1);
            context.API.VisibilityChanged += OnVisibilityChanged;
            return Task.CompletedTask;
        }

        public void OnVisibilityChanged(object _, VisibilityChangedEventArgs e)
        {
            if (context.CurrentPluginMetadata.Disabled || !e.IsVisible)
            {
                OneNoteApplication.ReleaseComObject();
            }
        }

        private static async Task OneNoteInitAsync(CancellationToken token = default)
        {
            if (semaphore.CurrentCount == 0 || OneNoteApplication.HasComObject)
                return;

            await semaphore.WaitAsync(token);
            OneNoteApplication.InitComObject();
            semaphore.Release();
        }
        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            var init = OneNoteInitAsync(token);

            if (string.IsNullOrEmpty(query.Search))
                return resultCreator.EmptyQuery();
            
            await init;

            return searchManager.Query(query);
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            return resultCreator.ContextMenu(selectedResult);
        }

        public System.Windows.Controls.Control CreateSettingPanel()
        {
            return new UI.Views.SettingsView(new UI.ViewModels.SettingsViewModel(context, settings, iconProvider));
        }

        public void Dispose()
        {
            context.API.VisibilityChanged -= OnVisibilityChanged;
            semaphore.Dispose();
            iconProvider.Dispose();
            OneNoteApplication.ReleaseComObject();
        }
    }
}