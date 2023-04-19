using System;
using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public class NotebookExplorer
    {
        private readonly ResultCreator rc;

        public NotebookExplorer(ResultCreator resultCreator)
        {
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

                currentParentItem = currentCollection.FirstOrDefault(item => item.Name == searchStrings[index]);
                if (currentParentItem == null)
                    return results;

                currentCollection = currentParentItem.Children;
            }

            var lastSearch = searchStrings[^1];

            var parentType = currentParentItem?.ItemType; //null if the current collection contains notebooks
            if (string.IsNullOrWhiteSpace(lastSearch))
            {
                results = currentCollection.Where(item => !ResultCreator.IsEncryptedSection(item))
                                           .Select(item => rc.GetOneNoteItemResult(item, true))
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


            if (lastSearch.StartsWith(Keywords.SearchByTitle) && (parentType == OneNoteItemType.Notebook || parentType == OneNoteItemType.SectionGroup || parentType == OneNoteItemType.Section))
            {
                results = rc.SearchByTitle(lastSearch, currentCollection, currentParentItem);
                return results;
            }

            if (lastSearch.StartsWith(Keywords.ScopedSearch) && (parentType == OneNoteItemType.Notebook || parentType == OneNoteItemType.SectionGroup))
            {
                results = ScopedSearch(oneNote, lastSearch, currentParentItem);
                return results;
            }

            List<int> highlightData = null;
            int score = 0;

            results = currentCollection.Where(item => !ResultCreator.IsEncryptedSection(item))
                                       .Where(item => rc.FuzzySearch(item.Name, lastSearch, out highlightData, out score))
                                       .Select(item => rc.GetOneNoteItemResult(item, true, highlightData, score))
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
            if (query.Length == Keywords.ScopedSearch.Length)
            {
                return ResultCreator.SingleResult($"Now searching all pages in \"{parentItem.Name}\"",
                                                  null,
                                                  Icons.Search);
            }

            string currentSearch = query[Keywords.SearchByTitle.Length..];
            var results = new List<Result>();

            results = oneNote.FindPages(parentItem, currentSearch)
                             .Select(pg => rc.CreatePageResult(pg, currentSearch))
                             .ToList();

            if (!results.Any())
                results = ResultCreator.NoMatchesFoundResult();

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
                        results.Add(rc.CreateNewSectionResult(query, parent));
                        results.Add(rc.CreateNewSectionGroupResult(query, parent));
                        break;
                    case OneNoteItemType.Section:
                        results.Add(rc.CreateNewPageResult(query, (OneNoteSection)parent));
                        break;
                    default:
                        break;
                }
            }
        }
    }
}