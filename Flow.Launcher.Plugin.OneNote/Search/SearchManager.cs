using System.Collections.Generic;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class SearchManager
	{
		private readonly TitleSearch titleSearch;
		private readonly NotebookExplorer notebookExplorer;
		private readonly DefaultSearch defaultSearch;
		private readonly RecentPages recentPages;

		public SearchManager(PluginInitContext context, Settings settings, ResultCreator resultCreator, VisibilityChanged visibilityChanged)
		{
			titleSearch = new TitleSearch(context, settings, resultCreator);
			notebookExplorer = new NotebookExplorer(context, settings, resultCreator, titleSearch, visibilityChanged);
			recentPages = new RecentPages(context, settings, resultCreator);
			defaultSearch = new DefaultSearch(context, settings, resultCreator);
		}

		public List<Result> Query(Query query)
		{
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