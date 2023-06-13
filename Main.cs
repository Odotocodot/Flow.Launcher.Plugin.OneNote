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

        private NotebookExplorer notebookExplorer;
        private ResultCreator rc;
        private static Settings settings;
      
        public void Init(PluginInitContext context)
        {
            this.context = context;
            settings = context.API.LoadSettingJsonStorage<Settings>();
            rc = new ResultCreator(context, settings);
            notebookExplorer = new NotebookExplorer(rc);
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
                    return oneNote.GetNotebooks()
                                  .GetPages()
                                  .OrderByDescending(pg => pg.LastModified)
                                  .Take(count)
                                  .Select(pg =>
                                  {
                                      Result result = rc.CreatePageResult(pg);
                                      result.SubTitleToolTip = result.SubTitle;
                                      result.SubTitle = $"{GetLastEdited(DateTime.Now - pg.LastModified)}\t{result.SubTitle}";
                                      result.IcoPath = Icons.RecentPage;
                                      return result;
                                  })
                                  .ToList();
                });
            }

            //Search via notebook structure
            if (query.FirstSearch.StartsWith(Keywords.NotebookExplorer))
            {
                return GetOneNote(oneNote =>
                {
                    return notebookExplorer.Explore(oneNote, query);
                });
            }
            //Search all items by title
            if(query.FirstSearch.StartsWith(Keywords.SearchByTitle))
            {
                var results = GetOneNote(oneNote => 
                {
                    return rc.SearchByTitle(string.Join(" ", query.SearchTerms), oneNote.GetNotebooks());
                });
                
                if (results.Any())
                    return results;
                    
                return ResultCreator.NoMatchesFoundResult();
            }

            //Check for invalid start of query i.e. symbols
            if (!char.IsLetterOrDigit(query.Search[0]))
            {
                return ResultCreator.SingleResult("Invalid query",
                                                  "The first character of the search must be a letter or a digit",
                                                  Icons.Warning);
            }

            //Default search 
            var searches = GetOneNote(oneNote =>
            {
                return oneNote.FindPages(query.Search)
                              .Select(pg => rc.CreatePageResult(pg, query.Search));
            });

            if (searches.Any())
                return searches.ToList();

            return ResultCreator.NoMatchesFoundResult();
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var results = new List<Result>();
            if(selectedResult.ContextData is IOneNoteItem item)
            {
                var result = GetOneNote(oneNote =>
                {
                    return rc.GetOneNoteItemResult(item, false);
                });
                result.Title = $"Open and sync \"{item.Name}\"";
                result.SubTitle = string.Empty;
                result.ContextData = null;
                results.Add(result);
            }
            return results;
        }

        private static string GetLastEdited(TimeSpan diff)
        {
            string lastEdited = "Last edited ";
            if (PluralCheck(diff.TotalDays, "day", ref lastEdited)
            || PluralCheck(diff.TotalHours, "hour", ref lastEdited)
            || PluralCheck(diff.TotalMinutes, "min", ref lastEdited)
            || PluralCheck(diff.TotalSeconds, "sec", ref lastEdited))
                return lastEdited;
            else
                return lastEdited += "Now.";

            static bool PluralCheck(double totalTime, string timeType, ref string lastEdited)
            {
                var roundedTime = (int)Math.Round(totalTime);
                if (roundedTime > 0)
                {
                    string plural = roundedTime == 1 ? "" : "s";
                    lastEdited += $"{roundedTime} {timeType}{plural} ago.";
                    return true;
                }
                else
                    return false;

            }
        }

        public System.Windows.Controls.Control CreateSettingPanel()
        {
            return new SettingsView(settings);
        }

        public static T GetOneNote<T>(Func<OneNoteApplication, T> action, Func<COMException, T> onException = null)
        {
            using var oneNote = new OneNoteApplication();
            return action(oneNote);
        }
    }
}