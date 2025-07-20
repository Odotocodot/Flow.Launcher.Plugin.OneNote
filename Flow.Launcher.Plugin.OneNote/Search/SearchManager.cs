using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class SearchManager
	{
		private readonly TitleSearch titleSearch;

		private readonly NotebookExplorer notebookExplorer;
		private readonly Settings settings;

		public SearchManager(Settings settings)
		{
			this.settings = settings;
			titleSearch = new TitleSearch
			{
				KeywordGetter = () => settings.Keywords.TitleSearch,
			};
			notebookExplorer = new NotebookExplorer
			{
				KeywordGetter = () => settings.Keywords.NotebookExplorer,
			};
			
		}
		public List<Result> Query(Query query)
		{
			//PluginState ps;
			var r = query.Search switch
			{
				{ } search when search.StartsWith(titleSearch.Keyword) => titleSearch.GetResults(search),
				//string search when search.StartsWith(settings.Keywords.TitleSearch) => TitleSearch(ps, settings.Keywords.TitleSearch, search)

			};
			return null;
		}
	}
	
	public record PluginState(PluginInitContext Context, Settings Settings, ResultCreator ResultCreator)
	{
		public Keywords Keywords => Settings.Keywords;
	}
}