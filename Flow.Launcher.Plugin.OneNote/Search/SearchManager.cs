using System.Collections.Generic;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class SearchManager
	{
		private readonly Settings settings;
		
		private readonly TitleSearch titleSearch;
		private readonly NotebookExplorer notebookExplorer;
		private readonly DefaultSearch defaultSearch;
		private readonly RecentPages recentPages;

		public SearchManager(PluginInitContext context, Settings settings, ResultCreator resultCreator)
		{
			this.settings = settings;
			titleSearch = new TitleSearch(context, settings, resultCreator);
			notebookExplorer = new NotebookExplorer(context, settings, resultCreator, titleSearch);
			recentPages = new RecentPages(context, settings, resultCreator);
			defaultSearch = new DefaultSearch(context, settings, resultCreator);

		}
		
		public List<Result> Query(string search)
		{
			return search switch
			{
				{ } when search.StartsWith(titleSearch.Keyword) => titleSearch.GetResults(search),
				{ } when search.StartsWith(notebookExplorer.Keyword) => notebookExplorer.GetResults(search),
				{ } when search.StartsWith(recentPages.Keyword) => recentPages.GetResults(search),
				_ => defaultSearch.GetResults(search!),
			};
		}
	}
	
	// public record PluginState(PluginInitContext Context, Settings Settings, ResultCreator ResultCreator)
	// {
	// 	public Keywords Keywords => Settings.Keywords;
	// }
}