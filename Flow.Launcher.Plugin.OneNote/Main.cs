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

        private SearchManager searchManager;
        private Settings settings;

        private static SemaphoreSlim semaphore;
        public Task InitAsync(PluginInitContext context)
        {
            this.context = context;
            settings = context.API.LoadSettingJsonStorage<Settings>();
            Icons.Init(context, settings);
            searchManager = new SearchManager(context, settings, new ResultCreator(context, settings));
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
                return searchManager.EmptyQuery();
            
            await init;

            return query.FirstSearch switch
            {
                string fs when fs.StartsWith(settings.Keywords.RecentPages) => searchManager.RecentPages(fs),
                string fs when fs.StartsWith(settings.Keywords.NotebookExplorer) => searchManager.NotebookExplorer(query),
                string fs when fs.StartsWith(settings.Keywords.TitleSearch) => searchManager.TitleSearch(string.Join(' ', query.SearchTerms), OneNoteApplication.GetNotebooks()),
                _ => searchManager.DefaultSearch(query.Search)
            };
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            return searchManager.ContextMenu(selectedResult);
        }

        public System.Windows.Controls.Control CreateSettingPanel()
        {
            return new UI.Views.SettingsView(new UI.ViewModels.SettingsViewModel(context, settings));
        }

        public void Dispose()
        {
            context.API.VisibilityChanged -= OnVisibilityChanged;
            semaphore.Dispose();
            Icons.Close();
            OneNoteApplication.ReleaseComObject();
        }
    }
}