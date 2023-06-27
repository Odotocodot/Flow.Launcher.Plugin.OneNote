using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using Odotocodot.OneNote.Linq;
namespace Flow.Launcher.Plugin.OneNote
{
    public class OneNotePlugin : IPlugin, IContextMenu, ISettingProvider
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
        }
        
        public List<Result> Query(Query query)
        {
            if (string.IsNullOrEmpty(query.Search))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Search OneNote pages",
                        SubTitle = $"Type \"{Keywords.NotebookExplorer}\" or select this option to search by notebook structure ",
                        AutoCompleteText = $"{query.ActionKeyword} {Keywords.NotebookExplorer}",
                        IcoPath = Icons.Logo,
                        Score = 2000,
                        Action = c =>
                        {
                            context.API.ChangeQuery($"{query.ActionKeyword} {Keywords.NotebookExplorer}");
                            return false;
                        },
                    },
                    new Result
                    {
                        Title = "See recent pages",
                        SubTitle = $"Type \"{Keywords.RecentPages}\" or select this option to see recently modified pages",
                        AutoCompleteText = $"{query.ActionKeyword} {Keywords.RecentPages}",
                        IcoPath = Icons.Recent,
                        Score = -1000,
                        Action = c =>
                        {
                            context.API.ChangeQuery($"{query.ActionKeyword} {Keywords.RecentPages}");
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
                            GetOneNote(oneNote =>
                            {
                                oneNote.CreateQuickNote();
                                return 0;
                            });
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
                            GetOneNote(oneNote =>
                            {
                                foreach (var notebook in oneNote.GetNotebooks())
                                {
                                    oneNote.SyncItem(notebook);
                                }
                                oneNote.OpenInOneNote(oneNote.GetNotebooks().First());
                                return 0;
                            });
                            return true;
                        }
                    },
                };
            }

            if (query.FirstSearch.StartsWith(Keywords.RecentPages))
            {
                int count = settings.DefaultRecentsCount;
                if (query.FirstSearch.Length > Keywords.RecentPages.Length && int.TryParse(query.FirstSearch[Keywords.RecentPages.Length..], out int userChosenCount))
                    count = userChosenCount;

                return GetOneNote(oneNote =>
                {
                    return searchManager.RecentPages(oneNote, count);
                });
            }

            //Search via notebook structure
            if (query.FirstSearch.StartsWith(Keywords.NotebookExplorer))
            {
                return GetOneNote(oneNote =>
                {
                    return searchManager.Explore(oneNote, query);
                });
            }
            //Search all items by title
            if(query.FirstSearch.StartsWith(Keywords.SearchByTitle))
            {
                return GetOneNote(oneNote => 
                {
                    return searchManager.TitleSearch(string.Join(" ", query.SearchTerms), oneNote.GetNotebooks());
                });
            }
            //Default search 
            return GetOneNote(oneNote =>
            {
                return searchManager.DefaultSearch(oneNote, query.Search);
            });
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            return searchManager.ContextMenu(selectedResult);
        }

        public System.Windows.Controls.Control CreateSettingPanel()
        {
            return new UI.SettingsView(new UI.SettingsViewModel(settings));
        }

        public static T GetOneNote<T>(Func<OneNoteApplication, T> action, Func<COMException, T> onException = null)
        {
            using var oneNote = new OneNoteApplication();
            return action(oneNote);
        }
    }
}