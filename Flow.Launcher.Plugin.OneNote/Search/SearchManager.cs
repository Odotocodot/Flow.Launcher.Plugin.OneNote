using System.Collections.Generic;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class SearchManager
	{
		private readonly TitleSearch titleSearch;
		private readonly NotebookExplorer notebookExplorer;
		private readonly StandardSearch standardSearch;
		private readonly RecentPages recentPages;
		public RootCache RootCache { get; }

		public SearchManager(PluginInitContext context, Settings settings, ResultCreator resultCreator)
		{
			RootCache = new RootCache();
			titleSearch = new TitleSearch(context, settings, resultCreator, RootCache);
			notebookExplorer = new NotebookExplorer(context, settings, resultCreator, RootCache);
			recentPages = new RecentPages(context, settings, resultCreator, RootCache);
			standardSearch = new StandardSearch(context, settings, resultCreator);
		}

		public List<Result> Query(Query query)
		{
			if (query.IsReQuery)
			{
				RootCache.SetDirty();
			}
			string search = query.Search;
			return search switch
			{
				{ } when search.StartsWithOrd(titleSearch.keyword) => titleSearch.GetResults(query),
				{ } when search.StartsWithOrd(notebookExplorer.keyword) => notebookExplorer.GetResults(query),
				{ } when search.StartsWithOrd(recentPages.keyword) => recentPages.GetResults(query),
				_ => standardSearch.GetResults(query),
			};
		}
	}
}