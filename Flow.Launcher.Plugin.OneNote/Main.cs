using System;
using System.Collections.Generic;
using System.Linq;
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
                OneNoteApplication.ReleaseComInstance();
            }
        }

        private static async Task OneNoteInitAsync(CancellationToken token = default)
        {
            if (semaphore.CurrentCount == 0 || OneNoteApplication.HasComInstance)
                return;

            await semaphore.WaitAsync(token);
            OneNoteApplication.Init();
            semaphore.Release();
        }
        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            var init = OneNoteInitAsync(token);
            if (string.IsNullOrEmpty(query.Search))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Search OneNote pages",
                        SubTitle = $"Type \"{settings.Keywords.NotebookExplorer}\" or select this option to search by notebook structure ",
                        AutoCompleteText = $"{query.ActionKeyword} {settings.Keywords.NotebookExplorer}",
                        IcoPath = Icons.Logo,
                        Score = 2000,
                        Action = c =>
                        {
                            context.API.ChangeQuery($"{query.ActionKeyword} {settings.Keywords.NotebookExplorer}");
                            return false;
                        },
                    },
                    new Result
                    {
                        Title = "See recent pages",
                        SubTitle = $"Type \"{settings.Keywords.RecentPages}\" or select this option to see recently modified pages",
                        AutoCompleteText = $"{query.ActionKeyword} {settings.Keywords.RecentPages}",
                        IcoPath = Icons.Recent,
                        Score = -1000,
                        Action = c =>
                        {
                            context.API.ChangeQuery($"{query.ActionKeyword} {settings.Keywords.RecentPages}");
                            return false;
                        },
                    },
                    new Result
                    {
                        Title = "New quick note",
                        IcoPath = Icons.NewPage,
                        Score = -4000,
                        Action = c =>
                        {
                            OneNoteApplication.CreateQuickNote();
                            return true;
                        }
                    },
                    new Result
                    {
                        Title = "Open and sync notebooks",
                        IcoPath = Icons.Sync,
                        Score = int.MinValue,
                        Action = c =>
                        {
                            foreach (var notebook in OneNoteApplication.GetNotebooks())
                            {
                                OneNoteApplication.SyncItem(notebook);
                            }
                            OneNoteApplication.GetNotebooks().First().OpenInOneNote();
                            return true;
                        }
                    },
                };
            }
            
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
            return new UI.SettingsView(new UI.SettingsViewModel(context, settings));
        }

        public void Dispose()
        {
            context.API.VisibilityChanged -= OnVisibilityChanged;
            semaphore.Dispose();
            Icons.Close();
            OneNoteApplication.ReleaseComInstance();
        }
    }
}