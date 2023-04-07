using System;
using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public class NotebookExplorer
    {
        private readonly PluginInitContext context;
        private readonly ResultCreator rc;

        public NotebookExplorer(PluginInitContext context, ResultCreator resultCreator)
        {
            this.context = context;
            rc = resultCreator;
        }

        public List<Result> Explore(OneNoteProvider oneNote, Query query)
        {
            var results = new List<Result>();

            string search = query.Search.Remove(query.Search.IndexOf(Keywords.NotebookExplorer), Keywords.NotebookExplorer.Length);

            string[] searchStrings = search.Split('\\', StringSplitOptions.None);

            IOneNoteItem currentParentItem = null;
            IEnumerable<IOneNoteItem> currentCollection = oneNote.Notebooks;


            for (int i = 0; i < searchStrings.Length; i++)
            {
                int index = i - 1;
                if (index < 0)
                    continue;

                if (!ValidateItem(currentCollection, searchStrings[index], out currentParentItem))
                    return results;

                currentCollection = currentParentItem.Children;
            }

            var lastSearch = searchStrings[^1];

            var parentType = currentParentItem?.ItemType; //null if the current collection contains notebooks
            if (string.IsNullOrWhiteSpace(lastSearch))
            {
                results = currentCollection.Where(item => !IsEncryptedSection(item))
                                           .Select(item => rc.GetOneNoteItemResult(oneNote, item, true))
                                           .ToList();

                if (!results.Any())
                {
                    switch (parentType)
                    {
                        case OneNoteItemType.Notebook:
                        case OneNoteItemType.SectionGroup:
                            //can create section/section group
                            results.Add(NoItemsInCollectionResult("section", Icons.NewSection, "(unencrypted) section"));
                            results.Add(NoItemsInCollectionResult("section group", Icons.NewSectionGroup));
                            break;
                        case OneNoteItemType.Section:
                            //can create page
                            results.Add(NoItemsInCollectionResult("page", Icons.NewPage));
                            break;
                        default:
                            break;
                    }

                }

                return results;
            }

            List<int> highlightData = null;
            int score = 0;

            if (lastSearch.StartsWith(Keywords.SearchByTitle) && (parentType == OneNoteItemType.Notebook || parentType == OneNoteItemType.SectionGroup || parentType == OneNoteItemType.Section))
            {
                return SearchByTitle(oneNote, lastSearch, currentParentItem, currentCollection);
            }

            if (lastSearch.StartsWith(Keywords.ScopedSearch) && (parentType == OneNoteItemType.Notebook || parentType == OneNoteItemType.SectionGroup))
            {
                return ScopedSearch(oneNote, lastSearch, currentParentItem);
            }

            results = currentCollection.Where(item => !IsEncryptedSection(item))
                                       .Where(item => MatchItem(item, lastSearch, out highlightData, out score))
                                       .Select(item => rc.GetOneNoteItemResult(oneNote, item, true, highlightData, score))
                                       .ToList();
            

            AddNewOneNoteItemResults(oneNote, results, currentParentItem, lastSearch);
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
        private List<Result> ScopedSearch(OneNoteProvider oneNote, string query, IOneNoteItem parentItem)
        {
            var results = new List<Result>();
            if (query.Length == Keywords.ScopedSearch.Length)
            {
                return new List<Result>
                    {
                        new Result
                        {
                            Title = $"Now searching all pages in \"{parentItem.Name}\"",
                            IcoPath = Icons.Search,
                        }
                    };
            }

            var currentSearch = query[Keywords.SearchByTitle.Length..];

            results = oneNote.FindPages(parentItem, currentSearch)
                             .Select(pg => rc.CreatePageResult(oneNote, pg, context.API.FuzzySearch(currentSearch, pg.Name).MatchData))
                             .ToList();

            if (!results.Any())
                results = ResultCreator.NoMatchesFoundResult();

            AddNewOneNoteItemResults(oneNote, results, parentItem, query);
            return results;
        }
        private List<Result> SearchByTitle(OneNoteProvider oneNote, string query, IOneNoteItem parentItem, IEnumerable<IOneNoteItem> currentCollection)
        {
            List<int> highlightData = null;
            int score = 0;
            var results = new List<Result>();
            if (query.Length == Keywords.SearchByTitle.Length)
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = $"Now searching by title in \"{parentItem.Name}\"",
                        IcoPath = Icons.Search,
                    }
                };
            }
            var currentSearch = query[Keywords.SearchByTitle.Length..];

            results = currentCollection.Traverse(item =>
            {
                if (IsEncryptedSection(item))
                    return false;

                return MatchItem(item, currentSearch, out highlightData, out score);
            })
            .Select(item => rc.GetOneNoteItemResult(oneNote, item, true, highlightData, score))
            .ToList();

            if (!results.Any())
                results = ResultCreator.NoMatchesFoundResult();

            AddNewOneNoteItemResults(oneNote, results, parentItem, query);
            return results;
        }

        private void AddNewOneNoteItemResults(OneNoteProvider oneNote, List<Result> results, IOneNoteItem parent, string query)
        {
            if (!results.Any(result => string.Equals(query.Trim(), result.Title, StringComparison.OrdinalIgnoreCase)))
            {
                switch (parent?.ItemType)
                {
                    case null:
                        results.Add(rc.CreateNewNotebookResult(oneNote, query));
                        break;
                    case OneNoteItemType.Notebook:
                    case OneNoteItemType.SectionGroup:
                        results.Add(rc.CreateNewSectionResult(oneNote, query, parent));
                        results.Add(rc.CreateNewSectionGroupResult(oneNote, query, parent));
                        break;
                    case OneNoteItemType.Section:
                        results.Add(rc.CreateNewPageResult(oneNote, query, (OneNoteSection)parent));
                        break;
                    default:
                        break;
                }
            }
        }

        private static bool IsEncryptedSection(IOneNoteItem item)
        {
            if (item.ItemType == OneNoteItemType.Section)
            {
                return ((OneNoteSection)item).Encrypted;
            }
            return false;
        }
        private static bool ValidateItem(IEnumerable<IOneNoteItem> items, string query, out IOneNoteItem item)
        {
            item = items.FirstOrDefault(t => t.Name == query);
            return item != null;
        }



        private bool MatchItem(IOneNoteItem item, string query, out List<int> highlightData, out int score)
        {
            return MatchScore(item.Name, query, out highlightData, out score);
        }

        private bool MatchScore(string itemName, string searchString, out List<int> highlightData, out int score)
        {
            var matchResult = context.API.FuzzySearch(searchString, itemName);
            highlightData = matchResult.MatchData;
            score = matchResult.Score;
            return matchResult.IsSearchPrecisionScoreMet();
        }
    }
}