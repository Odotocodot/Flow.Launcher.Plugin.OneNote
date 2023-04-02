using System;
using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public class NotebookExplorer
    {
        private readonly PluginInitContext context;
        private readonly OneNoteProvider oneNote;
        private readonly ResultCreator rc;

        public NotebookExplorer(PluginInitContext context, OneNoteProvider oneNote, ResultCreator resultCreator)
        {
            this.context = context;
            this.oneNote = oneNote;
            rc = resultCreator;
        }

        public List<Result> Explore(Query query)
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
                results = currentCollection.Where(item => HideEncryptedSections(item))
                                           .Select(item => rc.GetOneNoteItemResult(item, true))
                                           .ToList();

                if(!results.Any())
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
            results = currentCollection.Where(item => HideEncryptedSections(item))
                                       .Where(item => MatchItem(item, lastSearch, out highlightData, out score))
                                       .Select(item => rc.GetOneNoteItemResult(item, true, highlightData, score))
                                       .ToList();

            if (!results.Any(result => string.Equals(lastSearch.Trim(), result.Title, StringComparison.OrdinalIgnoreCase)))
            {
                AddNewOneNoteItemResults(results, lastSearch, parentType, currentParentItem);
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

        private static bool HideEncryptedSections(IOneNoteItem item)
        {
            if (item.ItemType == OneNoteItemType.Section)
            {
                return !((OneNoteSection)item).Encrypted;
            }
            return true;
        }
        private static bool ValidateItem(IEnumerable<IOneNoteItem> items, string query, out IOneNoteItem item)
        {
            item = items.FirstOrDefault(t => t.Name == query);
            return item != null;
        }

        private void AddNewOneNoteItemResults(List<Result> results, string currentQuery, OneNoteItemType? parentItemType, IOneNoteItem parent)
        {
            switch (parentItemType)
            {
                case null:
                    results.Add(rc.CreateNewNotebookResult(currentQuery));
                    break;
                case OneNoteItemType.Notebook:
                case OneNoteItemType.SectionGroup:
                    results.Add(rc.CreateNewSectionResult(currentQuery, parent));
                    results.Add(rc.CreateNewSectionGroupResult(currentQuery, parent));
                    break;
                case OneNoteItemType.Section:
                    results.Add(rc.CreateNewPageResult(currentQuery, (OneNoteSection)parent));
                    break;
                default:
                    break;
            }
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