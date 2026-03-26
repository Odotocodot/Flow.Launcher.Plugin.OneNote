using System.Collections.Generic;
using System.Linq;
using LinqToOneNote;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class TitleSearch(PluginInitContext context, Settings settings, ResultCreator resultCreator, RootCache rootCache)
		: SearchBase(context, settings, resultCreator, settings.Keywords.TitleSearch)
	{
		public override List<Result> GetResults(Query query) => GetResults(query.Search, null, rootCache.Root.Notebooks);

		public List<Result> GetResults(string query, IOneNoteItem? parent, IEnumerable<IOneNoteItem> collection)
		{
			if (query.Length == keyword.Length)
				return resultCreator.SearchType("Now searching by title", parent?.Name);

			var currentSearch = query[keyword.Length..];

			bool sortByLastModified = false;
			var lastModifiedKeyword = Keywords.RecentPages;
			if (currentSearch.StartsWithOrd(lastModifiedKeyword))
			{
				if (currentSearch.Length == lastModifiedKeyword.Length)
				{
					return resultCreator.SearchType("Now searching by title ordered by last modified", parent?.Name);
				}
				currentSearch = currentSearch[lastModifiedKeyword.Length..];
				sortByLastModified = true;
			}

			var results = collection.Descendants()
			                        .FilterBySettings(settings)
			                        .FuzzySearch(currentSearch, context)
			                        .OrderByLastModified(sortByLastModified)
			                        .Select(x =>
			                        {
				                        Result result = resultCreator.CreateOneNoteItemResult(x.Item, false, x.HighlightData, x.Score);
				                        result.AddSelectedCount = !sortByLastModified; 
				                        return result;
			                        })
			                        .ToList();

			return results.Count != 0 ? results : ResultCreator.NoMatchesFound();
		}
	}
}