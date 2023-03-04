using System;
using System.Collections.Generic;
using ScipBe.Common.Office.OneNote;
using System.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public class OneNotePlugin : IPlugin, IContextMenu
    {
        private PluginInitContext context;
        private bool hasOneNote;
        private readonly int recentPagesCount = 5;
        public IOneNoteExtNotebook lastSelectedNotebook;
        public IOneNoteExtSection lastSelectedSection;

        private NotebookExplorer notebookExplorer;
        private ResultCreator rc;

        public void Init(PluginInitContext context)
        {
            this.context = context;
            try
            {
                _ = OneNoteProvider.PageItems.Any();
                hasOneNote = true;
            }
            catch (Exception)
            {
                hasOneNote = false;
                return;
            }
            rc = new ResultCreator(context, this);
            notebookExplorer = new NotebookExplorer(context, this, rc);
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
            if (string.IsNullOrEmpty(query.Search))
            {
                return new List<Result>()
                {
                    new Result
                    {
                        Title = "Search OneNote pages",
                        SubTitle = $"Type \"{Keywords.NotebookExplorer}\" to search by notebook structure or select this option",
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
                        SubTitle = $"Type \"{Keywords.RecentPages}\" to see last modified pages or select this option",
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
                            ScipBeExtensions.CreateAndOpenPage();
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
                            OneNoteProvider.NotebookItems.OpenAndSync(OneNoteProvider.PageItems.First());
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

                return OneNoteProvider.PageItems.OrderByDescending(pg => pg.LastModified)
                    .Take(count)
                    .Select(pg =>
                    {
                        Result result = rc.CreatePageResult(pg);
                        result.SubTitle = $"{GetLastEdited(DateTime.Now - pg.LastModified)}\t{result.SubTitle}";
                        result.IcoPath = Icons.RecentPage;
                        return result;
                    })
                    .ToList();
            }

            //Search via notebook structure
            //NOTE: There is no nested sections i.e. there is nothing for the Section Group in the structure 
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
            var searches = OneNoteProvider.FindPages(query.Search)
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
            List<Result> results = new List<Result>();
            switch (selectedResult.ContextData)
            {
                case IOneNoteExtNotebook notebook:
                    Result result = rc.CreateNotebookResult(notebook);
                    result.Title = "Open and sync notebook";
                    result.SubTitle = notebook.Name;
                    result.ContextData = null;
                    result.Action = c =>
                    {
                        notebook.OpenAndSync();
                        lastSelectedNotebook = null;
                        return true;
                    };
                    results.Add(result);
                    break;
                case IOneNoteExtSection section:
                    Result sResult = rc.CreateSectionResult(section, lastSelectedNotebook);
                    sResult.Title = "Open and sync section";
                    sResult.SubTitle = section.Name;
                    sResult.ContextData = null;
                    sResult.Action = c =>
                    {
                        section.OpenAndSync();
                        lastSelectedNotebook = null;
                        lastSelectedSection = null;
                        return true;
                    };
                    Result nbResult = rc.CreateNotebookResult(lastSelectedNotebook);
                    nbResult.Title = "Open and sync notebook";
                    nbResult.SubTitle = lastSelectedNotebook.Name;
                    nbResult.Action = c =>
                    {
                        lastSelectedNotebook.OpenAndSync();
                        lastSelectedNotebook = null;
                        lastSelectedSection = null;
                        return true;
                    };
                    results.Add(sResult);
                    results.Add(nbResult);
                    break;
            }
            return results;
        }

        private static string GetLastEdited(TimeSpan diff)
        {
            string lastEdited = "Last editied ";
            if (PluralCheck(diff.TotalDays, "day", ref lastEdited)
            || PluralCheck(diff.TotalHours, "hour", ref lastEdited)
            || PluralCheck(diff.TotalMinutes, "min", ref lastEdited)
            || PluralCheck(diff.TotalSeconds, "sec", ref lastEdited))
                return lastEdited;
            else
                return lastEdited += "Now.";

            bool PluralCheck(double totalTime, string timeType, ref string lastEdited)
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