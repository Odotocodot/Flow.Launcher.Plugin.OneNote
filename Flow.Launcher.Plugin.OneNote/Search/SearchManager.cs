using System.Collections.Generic;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class SearchManager
	{
		private readonly Keywords keywords;
		private readonly NotebookExplorer notebookExplorer;
		private readonly StandardSearch standardSearch;
		private readonly TitleSearch titleSearch;
		public RootCache RootCache { get; }

		public SearchManager(PluginInitContext context, Settings settings, ResultCreator resultCreator)
		{
			standardSearch = new StandardSearch(context, settings, resultCreator);
			titleSearch = new TitleSearch(context, settings, resultCreator);
			notebookExplorer = new NotebookExplorer(context, settings, resultCreator, titleSearch, standardSearch);
			
			RootCache = new RootCache();
			keywords = settings.Keywords;
		}

		public List<Result> Query(Query query)
		{
			if (query.IsReQuery)
			{
				RootCache.SetDirty();
			}

			var searchData = new SearchData(query.Search, null, RootCache.Root.Notebooks, SortOrder.Default);
			Keyword? keyword = SearchExtensions.GetKeyword(ref searchData, keywords);

			SearchBase searchBase = keyword switch
			{
				not null when keyword == keywords.NotebookExplorer => notebookExplorer,
				not null when keyword == keywords.TitleSearch => titleSearch,
				_ => standardSearch,
			};
			
			var results = searchBase.GetResults(ref searchData);
			
			return results.Count != 0 ? results : ResultCreator.NoMatchesFound();
		}
	}
}