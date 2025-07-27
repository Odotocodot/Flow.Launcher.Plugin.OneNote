using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Flow.Launcher.Plugin.OneNote.Icons;
using Flow.Launcher.Plugin.OneNote.Search;
using Flow.Launcher.Plugin.OneNote.UI.Views;
using Odotocodot.OneNote.Linq;
namespace Flow.Launcher.Plugin.OneNote
{
    #nullable disable
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

        private void OnVisibilityChanged(object _, VisibilityChangedEventArgs e)
        {
            if (context.CurrentPluginMetadata.Disabled || !e.IsVisible)
            {
                Task.Run(OneNoteApplication.ReleaseComObject);
            }
        }

        private static async Task OneNoteInitAsync(CancellationToken token)
        {
            if (OneNoteApplication.HasComObject)
                return;
            
            if (!await semaphore.WaitAsync(0,token))
                return;
            
            try
            {
                OneNoteApplication.InitComObject();
            }
            finally
            {
                semaphore.Release();
            }
            
        }
        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            Task init = OneNoteInitAsync(token);

            if (string.IsNullOrEmpty(query.Search))
                return resultCreator.EmptyQuery();
            
            await init;

            return searchManager.Query(query.Search);
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            return resultCreator.ContextMenu(selectedResult);
        }

        public Control CreateSettingPanel()
        {
            return new SettingsView(context, settings, iconProvider);
        }

        public void Dispose()
        {
            context.API.VisibilityChanged -= OnVisibilityChanged;
            semaphore.Dispose();
            OneNoteApplication.ReleaseComObject();
        }
    }
}