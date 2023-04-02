using System;
using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public class OneNotePlugin : IPlugin, IContextMenu
    {
        private PluginInitContext context;
        private bool hasOneNote;
        private readonly int recentPagesCount = 5;

        private NotebookExplorer notebookExplorer;
        private ResultCreator rc;

        private OneNoteProvider oneNote;

        public void Init(PluginInitContext context)
        {
            this.context = context;
            // try
            // {
            //     _ = OneNoteProvider.PageItems.Any();
            //     hasOneNote = true;
            // }
            // catch (Exception)
            // {
            //     hasOneNote = false;
            //     return;
            // }
            hasOneNote = true;
            oneNote = new OneNoteProvider(true);
            rc = new ResultCreator(context, oneNote);
            notebookExplorer = new NotebookExplorer(context, oneNote, rc);
        }

        public List<Result> Query(Query query)
        {
            if (!hasOneNote)
            {
                return new List<Result>()
                {
                    new Result
                    {
                        Title = "OneNote is not installed.",
                        IcoPath = Icons.Unavailable
                    }
                };
            }
            oneNote.Init();
            if (string.IsNullOrEmpty(query.Search))
            {
                return new List<Result>()
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
                            oneNote.CreateQuickNote();
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
                            foreach (var notebook in oneNote.Notebooks)
                            {
                                oneNote.SyncItem(notebook);
                            }
                            oneNote.OpenInOneNote(oneNote.Notebooks.First());
                            return false;
                        }
                    },
                };
            }

            if (query.FirstSearch.StartsWith(Keywords.RecentPages))
            {
                int count = recentPagesCount;
                if (query.FirstSearch.Length > Keywords.RecentPages.Length && int.TryParse(query.FirstSearch[Keywords.RecentPages.Length..], out int userChosenCount))
                    count = userChosenCount;

                return oneNote.Pages.OrderByDescending(pg => pg.LastModified)
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
            }

            //Search via notebook structure
            if (query.FirstSearch.StartsWith(Keywords.NotebookExplorer))
                return notebookExplorer.Explore(query);

            //Check for invalid start of query i.e. symbols
            if (!char.IsLetterOrDigit(query.Search[0]))
                return new List<Result>()
                {
                    new Result
                    {
                        Title = "Invalid query",
                        SubTitle = "The first character of the search must be a letter or a digit",
                        IcoPath = Icons.Warning,
                    }
                };
            //Default search 
            var searches = oneNote.FindPages(query.Search)
                .Select(pg => rc.CreatePageResult(pg, context.API.FuzzySearch(query.Search, pg.Name).MatchData));

            if (searches.Any())
                return searches.ToList();
                
            return new List<Result>
            {
                new Result
                {
                    Title = "No matches found",
                    SubTitle = "Try searching something else, or syncing your notebooks.",
                    IcoPath = Icons.Logo,
                }
            };
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var results = new List<Result>();
            if(selectedResult.ContextData is IOneNoteItem item)
            {
                var result = rc.GetOneNoteItemResult(item, false);
                result.Title = $"Open and sync {item.Name}";
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
    }
}