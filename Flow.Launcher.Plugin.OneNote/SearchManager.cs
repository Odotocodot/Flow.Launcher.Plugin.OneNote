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
        private readonly ResultCreator resultCreator;
        private readonly NotebookExplorer notebookExplorer;

        public SearchManager(PluginInitContext context, Settings settings, ResultCreator resultCreator)
        {
            this.context = context;
            this.settings = settings;
            this.resultCreator = resultCreator;
            notebookExplorer = new NotebookExplorer(this, resultCreator);
        }

        internal List<Result> Query(Query query)
        {
            return query.Search switch
            {
                string s when s.StartsWith(settings.Keywords.RecentPages)
                    => RecentPages(s),

                string s when s.StartsWith(settings.Keywords.NotebookExplorer)
                    => notebookExplorer.Query(query),

                string s when s.StartsWith(settings.Keywords.TitleSearch)
                    => TitleSearch(s, null, OneNoteApplication.GetNotebooks()),

                _ => DefaultSearch(query.Search),
            };
        }

        private List<Result> DefaultSearch(string query)
        {
            // Check for invalid start of query i.e. symbols
            if (!char.IsLetterOrDigit(query[0]))
            {
                return resultCreator.InvalidQuery();
            }

            var results = OneNoteApplication.FindPages(query)
                                            .Select(pg => resultCreator.CreatePageResult(pg, query));

            return results.Any() ? results.ToList() : ResultCreator.NoMatchesFound();
        }

        private List<Result> TitleSearch(string query, IOneNoteItem parent, IEnumerable<IOneNoteItem> currentCollection)
        {
            if (query.Length == settings.Keywords.TitleSearch.Length && parent == null)
            {
                return resultCreator.SearchingByTitle();
            }

            List<int> highlightData = null;
            int score = 0;

            var currentSearch = query[settings.Keywords.TitleSearch.Length..];

            var results = currentCollection.Traverse(item => SettingsCheck(item) && FuzzySearch(item.Name, currentSearch, out highlightData, out score))
                                           .Select(item => resultCreator.CreateOneNoteItemResult(item, false, highlightData, score))
                                           .ToList();

            return results.Any() ? results : ResultCreator.NoMatchesFound();
        }

        private List<Result> RecentPages(string query)
        {
            int count = settings.DefaultRecentsCount;
            
            if (query.Length > settings.Keywords.RecentPages.Length && int.TryParse(query[settings.Keywords.RecentPages.Length..], out int userChosenCount))
                count = userChosenCount;
            
            return OneNoteApplication.GetNotebooks()
                                     .GetPages()
                                     .Where(SettingsCheck)
                                     .OrderByDescending(pg => pg.LastModified)
                                     .Take(count)
                                     .Select(resultCreator.CreateRecentPageResult)
                                     .ToList();
        }
        private bool FuzzySearch(string itemName, string search, out List<int> highlightData, out int score)
        {
            var matchResult = context.API.FuzzySearch(search, itemName);
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
        private sealed class NotebookExplorer
        {
            private readonly SearchManager searchManager;
            private readonly ResultCreator resultCreator;

            private Keywords Keywords => searchManager.settings.Keywords;
            internal NotebookExplorer(SearchManager searchManager, ResultCreator resultCreator)
            {
                this.searchManager = searchManager;
                this.resultCreator = resultCreator;
            }

            internal List<Result> Query(Query query)
            {
                var results = new List<Result>();

                string fullSearch = query.Search[(query.Search.IndexOf(Keywords.NotebookExplorer, StringComparison.Ordinal) + Keywords.NotebookExplorer.Length)..];

                IOneNoteItem parent = null;
                IEnumerable<IOneNoteItem> collection = OneNoteApplication.GetNotebooks();

                string[] searches = fullSearch.Split(Keywords.NotebookExplorerSeparator, StringSplitOptions.None);

                for (int i = -1; i < searches.Length - 1; i++)
                {
                    if (i < 0)
                    {
                        continue;
                    }

                    parent = collection.FirstOrDefault(item => item.Name.Equals(searches[i]));
                    if (parent == null)
                    {
                        return results;
                    }

                    collection = parent.Children;
                }

                string lastSearch = searches[^1];

                results = lastSearch switch
                {
                    // Empty search so show all in collection
                    string search when string.IsNullOrWhiteSpace(search)
                        => EmptySearch(parent, collection),

                    // Search by title
                    string search when search.StartsWith(Keywords.TitleSearch) && parent is not OneNotePage
                        => searchManager.TitleSearch(search, parent, collection),

                    // Scoped search
                    string search when search.StartsWith(Keywords.ScopedSearch) && parent is OneNoteNotebook or OneNoteSectionGroup
                        => ScopedSearch(search, parent),

                    // Default search
                    _ => Explorer(lastSearch, parent, collection),
                };

                if (parent != null)
                {
                    var result = resultCreator.CreateOneNoteItemResult(parent, false, score: 4000);
                    result.Title = $"Open \"{parent.Name}\" in OneNote";
                    result.SubTitle = lastSearch switch
                    {
                        string search when search.StartsWith(Keywords.TitleSearch)
                            => $"Now search by title in \"{parent.Name}\"",

                        string search when search.StartsWith(Keywords.ScopedSearch)
                            => $"Now searching all pages in \"{parent.Name}\"",

                        _ => $"Use \'{Keywords.ScopedSearch}\' to search this item. Use \'{Keywords.TitleSearch}\' to search by title in this item",
                    };

                    results.Add(result);
                }

                return results;
            }

            private List<Result> EmptySearch(IOneNoteItem parent, IEnumerable<IOneNoteItem> collection)
            {
                List<Result> results = collection.Where(searchManager.SettingsCheck)
                                                 .Select(item => resultCreator.CreateOneNoteItemResult(item, true))
                                                 .ToList();
                if (results.Any()) 
                    return results;
                return resultCreator.NoItemsInCollection(results, parent);
            }

            private List<Result> ScopedSearch(string query, IOneNoteItem parent)
            {
                if (query.Length == Keywords.ScopedSearch.Length)
                {
                    return ResultCreator.NoMatchesFound();
                }

                if (!char.IsLetterOrDigit(query[Keywords.ScopedSearch.Length]))
                {
                    return resultCreator.InvalidQuery();
                }

                string currentSearch = query[Keywords.TitleSearch.Length..];
                var results = new List<Result>();

                results = OneNoteApplication.FindPages(currentSearch, parent)
                                            .Select(pg => resultCreator.CreatePageResult(pg, currentSearch))
                                            .ToList();

                if (!results.Any())
                {
                    results = ResultCreator.NoMatchesFound();
                }

                return results;
            }
#nullable enable
            private List<Result> Explorer(string search, IOneNoteItem? parent, IEnumerable<IOneNoteItem> collection)
            {
                List<int>? highlightData = null;
                int score = 0;

                var results = collection.Where(searchManager.SettingsCheck)
                                        .Where(item => searchManager.FuzzySearch(item.Name, search, out highlightData, out score))
                                        .Select(item => resultCreator.CreateOneNoteItemResult(item, true, highlightData, score))
                                        .ToList();

                AddCreateNewOneNoteItemResults(search, parent, results);
                return results;
            }

            private void AddCreateNewOneNoteItemResults(string newItemName, IOneNoteItem? parent, List<Result> results)
            {
                if (!results.Any(result => string.Equals(newItemName.Trim(), result.Title, StringComparison.OrdinalIgnoreCase)))
                {
                    if (parent?.IsInRecycleBin() == true)
                    {
                        return;
                    }

                    switch (parent)
                    {
                        case null:
                            results.Add(resultCreator.CreateNewNotebookResult(newItemName));
                            break;
                        case OneNoteNotebook:
                        case OneNoteSectionGroup:
                            results.Add(resultCreator.CreateNewSectionResult(newItemName, parent));
                            results.Add(resultCreator.CreateNewSectionGroupResult(newItemName, parent));
                            break;
                        case OneNoteSection section:
                            if (!section.Locked)
                            {
                                results.Add(resultCreator.CreateNewPageResult(newItemName, section));
                            }

                            break;
                    }
                }
            }
        }
    }
}
