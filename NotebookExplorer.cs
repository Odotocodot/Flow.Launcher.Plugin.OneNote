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

            string[] searches = fullSearch.Split(Keywords.NotebookExplorerSeparator, StringSplitOptions.None);

            for (int i = -1; i < searches.Length -1; i++)
            {
                if (i < 0)
                    continue;

                parent = collection.FirstOrDefault(item => item.Name == searches[i]);
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
            //Search by title
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

                results = collection.Where(rc.SettingsCheck)
                                    .Where(item => rc.FuzzySearch(item.Name, lastSearch, out highlightData, out score))
                                    .Select(item => rc.CreateOneNoteItemResult(item, true, highlightData, score))
                                    .ToList();

                AddCreateNewOneNoteItemResults(oneNote, results, parent, lastSearch);
            }

            if(parent != null)
            {
                var result = rc.CreateOneNoteItemResult(parent, false, score: 4000);
                result.Title = $"Open \"{parent.Name}\" in OneNote";

                if(lastSearch.StartsWith(Keywords.SearchByTitle))
                    result.SubTitle = $"Now search by title in \"{parent.Name}\"";

                else if(lastSearch.StartsWith(Keywords.ScopedSearch))
                    result.SubTitle = $"Now searching all pages in \"{parent.Name}\"";

                else
                    result.SubTitle = $"Use \'{Keywords.ScopedSearch}\' to search this item. Use \'{Keywords.SearchByTitle}\' to search by title in this item";
                
                results.Add(result);
            }
            
            return results;
        }

        private List<Result> EmptySearch(IOneNoteItem parent, IEnumerable<IOneNoteItem> collection)
        {
            List<Result> results = collection.Where(rc.SettingsCheck)
                                             .Select(item => rc.CreateOneNoteItemResult(item, true))
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
                        if (!((OneNoteSection)parent).Locked)
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

            string currentSearch = query[Keywords.SearchByTitle.Length..];
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
                if (parent.IsInRecycleBin())
                    return;
                
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
                        var section = (OneNoteSection)parent;
                        if(!section.Locked)
                            results.Add(ResultCreator.CreateNewPageResult(query, section));
                        break;
                    default:
                        break;
                }
            }
        }
    }
}