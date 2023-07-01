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
                            });
                            return true;
                        }
                    },
                };
            }

            return GetOneNote(oneNote =>
            {
                return query.FirstSearch switch
                {
                    string fs when fs.StartsWith(Keywords.RecentPages) => searchManager.RecentPages(oneNote, fs),
                    string fs when fs.StartsWith(Keywords.NotebookExplorer) => searchManager.NotebookExplorer(oneNote, query),
                    string fs when fs.StartsWith(Keywords.TitleSearch) => searchManager.TitleSearch(string.Join(' ', query.SearchTerms), oneNote.GetNotebooks()),
                    _ => searchManager.DefaultSearch(oneNote, query.Search)
                };
            },context,query);
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            return searchManager.ContextMenu(selectedResult);
        }

        public System.Windows.Controls.Control CreateSettingPanel()
        {
            return new UI.SettingsView(new UI.SettingsViewModel(settings));
        }

        public static List<Result> GetOneNote(Func<OneNoteApplication, List<Result>> action, PluginInitContext context, Query query)
        {
            bool error = false;
            try
            {
                using var oneNote = new OneNoteApplication();
                return action(oneNote);
            }
            catch (Exception ex) when (ex is InvalidCastException || ex is COMException) 
            {
                //exceptions are randomly thrown when rapidly creating a new instance;
                error = true;
                return ResultCreator.SingleResult("Loading...", null, null);
            }
            finally
            {
                if(error)
                {
                    context.API.ChangeQuery(query.RawQuery, true);
                }
            }
        }
        public static void GetOneNote(Action<OneNoteApplication> action)
        {
            using var oneNote = new OneNoteApplication();
            action(oneNote);
        }
    }
}