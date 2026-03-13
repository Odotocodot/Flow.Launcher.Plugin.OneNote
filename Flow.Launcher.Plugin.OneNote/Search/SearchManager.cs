using System.Collections.Generic;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class SearchManager
	{
		private readonly TitleSearch titleSearch;
		private readonly NotebookExplorer notebookExplorer;
		private readonly DefaultSearch defaultSearch;
		private readonly RecentPages recentPages;
		private readonly RootCache rootCache;
		public RootCache RootCache => rootCache;

		public SearchManager(PluginInitContext context, Settings settings, ResultCreator resultCreator)
		{
			rootCache = new RootCache();
			titleSearch = new TitleSearch(context, settings, resultCreator, rootCache);
			notebookExplorer = new NotebookExplorer(context, settings, resultCreator, titleSearch, rootCache);
			recentPages = new RecentPages(context, settings, resultCreator, rootCache);
			defaultSearch = new DefaultSearch(context, settings, resultCreator);
		}

		public List<Result> Query(Query query)
		{
			if (query.IsReQuery)
			{
				rootCache.SetDirty();
			}
			string search = query.Search;
			return search switch
			{
				{ } when search.StartsWithOrd(titleSearch.keyword) => titleSearch.GetResults(query),
				{ } when search.StartsWithOrd(notebookExplorer.keyword) => notebookExplorer.GetResults(query),
				{ } when search.StartsWithOrd(recentPages.keyword) => recentPages.GetResults(query),
				_ => defaultSearch.GetResults(query),
			};
		}
	}
}