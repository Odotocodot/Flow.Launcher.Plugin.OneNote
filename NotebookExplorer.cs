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
    
        public List<Result> Explore(OneNoteApplication oneNote, Query query)
        {
            var results = new List<Result>();

            string fullSearch = query.Search.Remove(query.Search.IndexOf(Keywords.NotebookExplorer), Keywords.NotebookExplorer.Length);

            IOneNoteItem parent = null;
            IEnumerable<IOneNoteItem> collection = oneNote.GetNotebooks();

            string[] searches = fullSearch.Split('\\', StringSplitOptions.None);
            foreach (var search in searches)
            {
                parent = collection.FirstOrDefault(x => x.Name == search);
                if (parent == null)
                    return results;

                collection = parent.Children;
            }

            string lastSearch = searches[^1];

            //Empty search so show all in collection;
            if (string.IsNullOrEmpty(lastSearch))
            {
                results = EmptySearch(parent, collection);
            }
            //search by title
            else if (lastSearch.StartsWith(Keywords.SearchByTitle) && parent?.ItemType != OneNoteItemType.Page)
            {
                results = rc.SearchByTitle(lastSearch, collection, parent);
            }
            //Scoped search
            else if (lastSearch.StartsWith(Keywords.ScopedSearch) && (parent?.ItemType == OneNoteItemType.Notebook || parent?.ItemType == OneNoteItemType.SectionGroup))
            {
                results = ScopedSearch(oneNote, lastSearch, parent);
            }
            //Default search
            else 
            {
                List<int> highlightData = null;
                int score = 0;

                results = collection.Where(item => !ResultCreator.IsEncryptedSection(item))
                                        .Where(item => rc.FuzzySearch(item.Name, lastSearch, out highlightData, out score))
                                        .Select(item => rc.GetOneNoteItemResult(item, true, highlightData, score))
                                        .ToList();

                AddCreateNewOneNoteItemResults(oneNote, results, parent, lastSearch);
            }

            if(parent != null)
            {
                var result = rc.GetOneNoteItemResult(parent, false, score: int.MaxValue);
                result.Title = $"Open {parent.Name} in OneNote";
                results.Add(result);
            }
            
            return results;
        }

        private List<Result> EmptySearch(IOneNoteItem parent, IEnumerable<IOneNoteItem> collection)
        {
            List<Result> results = collection.Where(item => !ResultCreator.IsEncryptedSection(item))
                                .Select(item => rc.GetOneNoteItemResult(item, true))
                                .ToList();
            if (!results.Any())
            {
                switch (parent?.ItemType) //parent can be null if the collection contains notebooks.
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

        private List<Result> ScopedSearch(OneNoteApplication oneNote, string query, IOneNoteItem parentItem)
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

        private void AddCreateNewOneNoteItemResults(OneNoteApplication oneNote, List<Result> results, IOneNoteItem parent, string query)
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
                        results.Add(ResultCreator.CreateNewPageResult(query, (OneNoteSection)parent));
                        break;
                    default:
                        break;
                }
            }
        }
    }
}