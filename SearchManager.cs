using System;
using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public class SearchManager
    {
        private readonly PluginInitContext context;
        private readonly Settings settings;
        private readonly ResultCreator rc;

        public SearchManager(PluginInitContext context, Settings settings, ResultCreator resultCreator)
        {
            this.context = context;
            this.settings = settings;
            rc = resultCreator;
        }
        #region Notebook Explorer
        public List<Result> NotebookExplorer(OneNoteApplication oneNote, Query query)
        {
            var results = new List<Result>();

            string fullSearch = query.Search.Remove(query.Search.IndexOf(Keywords.NotebookExplorer), Keywords.NotebookExplorer.Length);

            IOneNoteItem parent = null;
            IEnumerable<IOneNoteItem> collection = oneNote.GetNotebooks();

            string[] searches = fullSearch.Split(Keywords.NotebookExplorerSeparator, StringSplitOptions.None);

            for (int i = -1; i < searches.Length - 1; i++)
            {
                if (i < 0)
                    continue;

                parent = collection.FirstOrDefault(item => item.Name == searches[i]);
                if (parent == null)
                    return results;

                collection = parent.Children;
            }

            string lastSearch = searches[^1];

            results = lastSearch switch
            {
                //Empty search so show all in collection
                string ls when string.IsNullOrWhiteSpace(ls) 
                    => NotebookEmptySearch(parent, collection),

                //Search by title
                string ls when ls.StartsWith(Keywords.TitleSearch) && parent is not OneNotePage
                    => TitleSearch(ls, collection, parent),

                //scoped search
                string ls when ls.StartsWith(Keywords.ScopedSearch) && (parent is OneNoteNotebook || parent is OneNoteSectionGroup)
                    => ScopedSearch(oneNote, ls, parent),

                //default search
                _ => NotebookDefaultSearch(oneNote, parent, collection, lastSearch)
            };

            if (parent != null)
            {
                var result = rc.CreateOneNoteItemResult(parent, false, score: 4000);
                result.Title = $"Open \"{parent.Name}\" in OneNote";

                if (lastSearch.StartsWith(Keywords.TitleSearch))
                {
                    result.SubTitle = $"Now search by title in \"{parent.Name}\"";
                }
                else if (lastSearch.StartsWith(Keywords.ScopedSearch))
                {
                    result.SubTitle = $"Now searching all pages in \"{parent.Name}\"";
                }
                else
                {
                    result.SubTitle = $"Use \'{Keywords.ScopedSearch}\' to search this item. Use \'{Keywords.TitleSearch}\' to search by title in this item";
                }

                results.Add(result);
            }

            return results;
        }

        private List<Result> NotebookDefaultSearch(OneNoteApplication oneNote, IOneNoteItem parent, IEnumerable<IOneNoteItem> collection, string lastSearch)
        {
            List<Result> results;
            List<int> highlightData = null;
            int score = 0;

            results = collection.Where(SettingsCheck)
                                .Where(item => FuzzySearch(item.Name, lastSearch, out highlightData, out score))
                                .Select(item => rc.CreateOneNoteItemResult(item, true, highlightData, score))
                                .ToList();

            AddCreateNewOneNoteItemResults(oneNote, results, parent, lastSearch);
            return results;
        }

        private List<Result> NotebookEmptySearch(IOneNoteItem parent, IEnumerable<IOneNoteItem> collection)
        {
            List<Result> results = collection.Where(SettingsCheck)
                                             .Select(item => rc.CreateOneNoteItemResult(item, true))
                                             .ToList();
            if (!results.Any())
            {
                switch (parent) //parent can be null if the collection contains notebooks.
                {
                    case OneNoteNotebook:
                    case OneNoteSectionGroup:
                        //can create section/section group
                        results.Add(NoItemsInCollectionResult("section", Icons.NewSection, "(unencrypted) section"));
                        results.Add(NoItemsInCollectionResult("section group", Icons.NewSectionGroup));
                        break;
                    case OneNoteSection section:
                        //can create page
                        if (!section.Locked)
                            results.Add(NoItemsInCollectionResult("page", Icons.NewPage));
                        break;
                    default:
                        break;
                }
            }

            return results;

            static Result NoItemsInCollectionResult(string title, string iconPath, string subTitle = null)
            {
                return new Result
                {
                    Title = $"Create {title}: \"\"",
                    SubTitle = $"No {subTitle ?? title}s found. Type a valid title to create one",
                    IcoPath = iconPath,
                };
            }
        }

        private List<Result> ScopedSearch(OneNoteApplication oneNote, string query, IOneNoteItem parent)
        {
            if (query.Length == Keywords.ScopedSearch.Length)
                return ResultCreator.NoMatchesFoundResult();

            if (!char.IsLetterOrDigit(query[Keywords.ScopedSearch.Length]))
                return ResultCreator.InvalidQuery();

            string currentSearch = query[Keywords.TitleSearch.Length..];
            var results = new List<Result>();

            results = oneNote.FindPages(parent, currentSearch)
                             .Select(pg => rc.CreatePageResult(pg, currentSearch))
                             .ToList();

            if (!results.Any())
                results = ResultCreator.NoMatchesFoundResult();

            return results;
        }

        private void AddCreateNewOneNoteItemResults(OneNoteApplication oneNote, List<Result> results, IOneNoteItem parent, string query)
        {
            if (!results.Any(result => string.Equals(query.Trim(), result.Title, StringComparison.OrdinalIgnoreCase)))
            {
                if (parent?.IsInRecycleBin() == true)
                    return;

                switch (parent)
                {
                    case null:
                        results.Add(rc.CreateNewNotebookResult(oneNote, query));
                        break;
                    case OneNoteNotebook:
                    case OneNoteSectionGroup:
                        results.Add(rc.CreateNewSectionResult(query, parent));
                        results.Add(rc.CreateNewSectionGroupResult(query, parent));
                        break;
                    case OneNoteSection section:
                        if (!section.Locked)
                            results.Add(ResultCreator.CreateNewPageResult(query, section));
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion
        public List<Result> DefaultSearch(OneNoteApplication oneNote, string query)
        {
            //Check for invalid start of query i.e. symbols
            if (!char.IsLetterOrDigit(query[0]))
            {
                return ResultCreator.InvalidQuery();
            }

            var results = oneNote.FindPages(query)
                                 .Select(pg => rc.CreatePageResult(pg, query));
                          
            if (results.Any())
                return results.ToList();

            return ResultCreator.NoMatchesFoundResult();
        }

        public List<Result> TitleSearch(string query, IEnumerable<IOneNoteItem> currentCollection, IOneNoteItem parent = null)
        {
            if (query.Length == Keywords.TitleSearch.Length && parent == null)
                return ResultCreator.SingleResult($"Now searching by title.", null, Icons.Search);

            List<int> highlightData = null;
            int score = 0;
            var results = new List<Result>();

            var currentSearch = query[Keywords.TitleSearch.Length..];

            results = currentCollection.Traverse(item =>
                                        {
                                            if (!SettingsCheck(item))
                                                return false;

                                            return FuzzySearch(item.Name, currentSearch, out highlightData, out score);
                                        })
                                        .Select(item => rc.CreateOneNoteItemResult(item, false, highlightData, score))
                                        .ToList();

            if (!results.Any())
                results = ResultCreator.NoMatchesFoundResult();

            return results;
        }
        public List<Result> RecentPages(OneNoteApplication oneNote, string query)
        {
            int count = settings.DefaultRecentsCount;
            if (query.Length > Keywords.RecentPages.Length && int.TryParse(query[Keywords.RecentPages.Length..], out int userChosenCount))
                count = userChosenCount;

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
        }

        public List<Result> ContextMenu(Result selectedResult)
        {
            var results = new List<Result>();
            if (selectedResult.ContextData is IOneNoteItem item)
            {
                var result = rc.CreateOneNoteItemResult(item, false);
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
            if (PluralCheck(diff.TotalDays,    "day",  ref lastEdited)
             || PluralCheck(diff.TotalHours,   "hour", ref lastEdited)
             || PluralCheck(diff.TotalMinutes, "min",  ref lastEdited)
             || PluralCheck(diff.TotalSeconds, "sec",  ref lastEdited))
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
        private bool FuzzySearch(string itemName, string searchString, out List<int> highlightData, out int score)
        {
            var matchResult = context.API.FuzzySearch(searchString, itemName);
            highlightData = matchResult.MatchData;
            score = matchResult.Score;
            return matchResult.IsSearchPrecisionScoreMet();
        }

        private bool SettingsCheck(IOneNoteItem item)
        {
            bool success = true;
            if (!settings.ShowEncrypted && item is OneNoteSection section)
                success = !section.Encrypted;

            if (!settings.ShowRecycleBin && item.IsInRecycleBin())
                success = false;
            return success;
        }

    }

}
