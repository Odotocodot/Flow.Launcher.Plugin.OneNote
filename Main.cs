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
                        IcoPath = Constants.UnavailableIconPath
                    }
                };
            }
            if (string.IsNullOrEmpty(query.Search))
            {
                var results = new List<Result>();
                results.Add(new Result
                {
                    Title = "Search OneNote pages",
                    SubTitle = $"Type \"{Constants.StructureKeyword}\" to search by notebook structure or select this option",
                    AutoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {Constants.StructureKeyword}",
                    IcoPath = Constants.LogoIconPath,
                    Score = 2000,
                    Action = c =>
                    {
                        context.API.ChangeQuery($"{context.CurrentPluginMetadata.ActionKeyword} {Constants.StructureKeyword}");
                        return false;
                    },
                });
                results.Add(new Result
                {
                    Title = "See recent pages",
                    SubTitle = $"Type \"{Constants.RecentKeyword}\" to see last modified pages or select this option",
                    AutoCompleteText = $"{context.CurrentPluginMetadata.ActionKeyword} {Constants.RecentKeyword}",
                    IcoPath = Constants.RecentIconPath,
                    Score = -1000,
                    Action = c =>
                    {
                        context.API.ChangeQuery($"{context.CurrentPluginMetadata.ActionKeyword} {Constants.RecentKeyword}");
                        return false;
                    },
                });
                results.Add(new Result
                {
                    Title = "Open and sync notebooks",
                    IcoPath = Constants.SyncIconPath,
                    Score = int.MinValue,
                    Action = c =>
                    {
                        OneNoteProvider.PageItems.First().OpenInOneNote();
                        OneNoteProvider.NotebookItems.Sync();
                        return false;
                    }
                });
                return results;
            }
            if (query.FirstSearch.StartsWith(Constants.RecentKeyword))
            {
                int count = recentPagesCount;
                if (query.FirstSearch.Length > Constants.RecentKeyword.Length && int.TryParse(query.FirstSearch[Constants.RecentKeyword.Length..], out int userChosenCount))
                    count = userChosenCount;

                return OneNoteProvider.PageItems.OrderByDescending(pg => pg.LastModified)
                    .Take(count)
                    .Select(pg =>
                    {
                        Result result = rc.CreatePageResult(pg);
                        result.SubTitle = $"{GetLastEdited(DateTime.Now - pg.LastModified)}\t{result.SubTitle}";
                        result.IcoPath = Constants.RecentPageIconPath;
                        return result;
                    })
                    .ToList();
            }

            //Search via notebook structure
            //NOTE: There is no nested sections i.e. there is nothing for the Section Group in the structure 
            if (query.FirstSearch.StartsWith(Constants.StructureKeyword))
                return notebookExplorer.Explore(query);

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
                    IcoPath = Constants.LogoIconPath,
                }
            };
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            switch (selectedResult.ContextData)
            {
                case IOneNoteExtNotebook notebook:
                    Result result = rc.CreateNotebookResult(notebook);
                    result.Title = "Open and sync notebook";
                    result.SubTitle = notebook.Name;
                    result.ContextData = null;
                    result.Action = c =>
                    {
                        notebook.Sections.First().Pages
                            .OrderByDescending(pg => pg.LastModified)
                            .First()
                            .OpenInOneNote();
                        notebook.Sync();
                        lastSelectedNotebook = null;
                        return true;
                    };
                    return new List<Result> { result };
                case IOneNoteExtSection section:
                    Result sResult = rc.CreateSectionResult(section, lastSelectedNotebook);
                    sResult.Title = "Open and sync section";
                    sResult.SubTitle = section.Name;
                    sResult.ContextData = null;
                    sResult.Action = c =>
                    {
                        section.Pages.OrderByDescending(pg => pg.LastModified)
                            .First()
                            .OpenInOneNote();
                        section.Sync();
                        lastSelectedNotebook = null;
                        lastSelectedSection = null;
                        return true;
                    };
                    Result nbResult = rc.CreateNotebookResult(lastSelectedNotebook);
                    nbResult.Title = "Open and sync notebook";
                    nbResult.SubTitle = lastSelectedNotebook.Name;
                    nbResult.Action = c =>
                    {
                        lastSelectedNotebook.Sections.First().Pages
                            .OrderByDescending(pg => pg.LastModified)
                            .First()
                            .OpenInOneNote();
                        lastSelectedNotebook.Sync();
                        lastSelectedNotebook = null;
                        lastSelectedSection = null;
                        return true;
                    };
                    return new List<Result> { sResult, nbResult, };
                default:
                    return new List<Result>();
            }
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