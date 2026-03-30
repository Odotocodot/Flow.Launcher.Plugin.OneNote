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
			const string emptySearchPrefix = "Now searching by title";
			if (query.Length == keyword.Length)
				//TODO return Searching by title and a Searching by last modified result with an action to auto complete last modified.
				return resultCreator.SearchType(emptySearchPrefix, parent?.Name);

			var currentSearch = query[keyword.Length..];

			var sortByLastModified = GetSortOrder(ref currentSearch, out var emptySearch);
			
			if(emptySearch)
				return resultCreator.SearchType($"{emptySearchPrefix} ordered by last modified", parent?.Name);;

			var results = collection.Descendants()
			                        .FilterBySettings(settings)
			                        .FuzzySearch(currentSearch, context)
			                        .Select(x => resultCreator.CreateOneNoteItemResult(x.Item, false, x.HighlightData, x.Score))
			                        .OrderByLastModified(sortByLastModified)
			                        .ToList();

			return results.Count != 0 ? results : ResultCreator.NoMatchesFound();
		}
	}
}