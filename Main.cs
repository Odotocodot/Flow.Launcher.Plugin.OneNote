using System;
using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;
namespace Flow.Launcher.Plugin.OneNote
{
    public class Main : IPlugin, IContextMenu, ISettingProvider, IDisposable
    {
        private PluginInitContext context;

        private SearchManager searchManager;
        private Settings settings;

        public void Init(PluginInitContext context)
        {
            this.context = context;
            settings = context.API.LoadSettingJsonStorage<Settings>();
            Icons.Init(context, settings);
            searchManager = new SearchManager(context, settings, new ResultCreator(context, settings));
            context.API.VisibilityChanged += OnVisibilityChanged;
        }

        public void OnVisibilityChanged(object _, VisibilityChangedEventArgs e)
        {
            if (context.CurrentPluginMetadata.Disabled || !e.IsVisible)
            {
                OneNoteApplication.ReleaseCOMInstance();
            }
        }
        
        public List<Result> Query(Query query)
        {
            OneNoteApplication.Init();
            if (string.IsNullOrEmpty(query.Search))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Search OneNote pages",
                        SubTitle = $"Type \"{settings.NotebookExplorerKeyword}\" or select this option to search by notebook structure ",
                        AutoCompleteText = $"{query.ActionKeyword} {settings.NotebookExplorerKeyword}",
                        IcoPath = Icons.Logo,
                        Score = 2000,
                        Action = c =>
                        {
                            context.API.ChangeQuery($"{query.ActionKeyword} {settings.NotebookExplorerKeyword}");
                            return false;
                        },
                    },
                    new Result
                    {
                        Title = "See recent pages",
                        SubTitle = $"Type \"{settings.RecentPagesKeyword}\" or select this option to see recently modified pages",
                        AutoCompleteText = $"{query.ActionKeyword} {settings.RecentPagesKeyword}",
                        IcoPath = Icons.Recent,
                        Score = -1000,
                        Action = c =>
                        {
                            context.API.ChangeQuery($"{query.ActionKeyword} {settings.RecentPagesKeyword}");
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

            return query.FirstSearch switch
            {
                string fs when fs.StartsWith(settings.RecentPagesKeyword) => searchManager.RecentPages(fs),
                string fs when fs.StartsWith(settings.NotebookExplorerKeyword) => searchManager.NotebookExplorer(query),
                string fs when fs.StartsWith(settings.TitleSearchKeyword) => searchManager.TitleSearch(string.Join(' ', query.SearchTerms), OneNoteApplication.GetNotebooks()),
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
            OneNoteApplication.ReleaseCOMInstance();
        }
    }
}