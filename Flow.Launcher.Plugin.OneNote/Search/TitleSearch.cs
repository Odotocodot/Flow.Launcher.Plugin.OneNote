using System.Collections.Generic;
using System.Linq;
using LinqToOneNote;
using OneNoteApp = LinqToOneNote.OneNote;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class TitleSearch(PluginInitContext context, Settings settings, ResultCreator resultCreator)
		: SearchBase(context, settings, resultCreator, settings.Keywords.TitleSearch)
	{
		public override List<Result> GetResults(Query query) => Filter(query.Search, null, OneNoteApp.GetFullHierarchy().Notebooks);

		public List<Result> Filter(string query, IOneNoteItem? parent, IEnumerable<IOneNoteItem> collection)
		{
			if (query.Length == keyword.Length)
				return resultCreator.SearchType("Now searching by title", parent?.Name);

			var currentSearch = query[keyword.Length..];

			var results = collection.Descendants()
			                        .FilterBySettings(settings)
			                        .FuzzySearch(currentSearch, context)
			                        .Select(x => resultCreator.CreateOneNoteItemResult(x.Item, false, x.HighlightData, x.Score))
			                        .ToList();

			return results.Count != 0 ? results : ResultCreator.NoMatchesFound();
		}
	}
}