using System.Collections.Generic;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public abstract class SearchBase(PluginInitContext context, Settings settings, ResultCreator resultCreator, Keyword keyword)
	{
		protected readonly PluginInitContext context = context;
		protected readonly Settings settings = settings;
		protected readonly ResultCreator resultCreator = resultCreator;
		public readonly Keyword keyword = keyword;
		protected Keywords Keywords => settings.Keywords;
		public abstract List<Result> GetResults(Query query);

		protected bool GetSortOrder(ref string currentSearch, out bool emptySearchOnSorted)
		{
			emptySearchOnSorted = false;
			Keyword lastModifiedKeyword = Keywords.RecentPages;
			if (!currentSearch.StartsWithOrd(lastModifiedKeyword))
				return false;
			if (currentSearch.Length == lastModifiedKeyword.Length)
			{
				emptySearchOnSorted = true;
			}
			currentSearch = currentSearch[lastModifiedKeyword.Length..];
			return true;
		}
		
		
	}
}