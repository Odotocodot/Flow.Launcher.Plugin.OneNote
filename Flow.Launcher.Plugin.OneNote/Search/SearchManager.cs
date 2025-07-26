using System.Collections.Generic;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class SearchManager
	{
		private readonly TitleSearch titleSearch;
		private readonly NotebookExplorer notebookExplorer;
		private readonly DefaultSearch defaultSearch;
		private readonly RecentPages recentPages;

		public SearchManager(PluginInitContext context, Settings settings, ResultCreator resultCreator)
		{
			titleSearch = new TitleSearch(context, settings, resultCreator);
			notebookExplorer = new NotebookExplorer(context, settings, resultCreator, titleSearch);
			recentPages = new RecentPages(context, settings, resultCreator);
			defaultSearch = new DefaultSearch(context, settings, resultCreator);

		}
		
		public List<Result> Query(string search)
		{
			return search switch
			{
				{ } when search.StartsWithOrd(titleSearch.Keyword) => titleSearch.GetResults(search),
				{ } when search.StartsWithOrd(notebookExplorer.Keyword) => notebookExplorer.GetResults(search),
				{ } when search.StartsWithOrd(recentPages.Keyword) => recentPages.GetResults(search),
				_ => defaultSearch.GetResults(search!),
			};
		}
	}
}