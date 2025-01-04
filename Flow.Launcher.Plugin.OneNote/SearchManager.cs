using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote
{
    public partial class SearchManager
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
    }
}
