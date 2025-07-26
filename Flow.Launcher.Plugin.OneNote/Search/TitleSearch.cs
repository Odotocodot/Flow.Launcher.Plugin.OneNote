using System;
using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class TitleSearch : SearchBase
	{
		public TitleSearch(PluginInitContext context, Settings settings, ResultCreator resultCreator) 
			: base(context, settings, resultCreator, settings.Keywords.TitleSearch) { }

		public override List<Result> GetResults(string query) => Filter(query, null, OneNoteApplication.GetNotebooks());

		public List<Result> Filter(string query, IOneNoteItem? parent, IEnumerable<IOneNoteItem> collection)
		{
			if (query.Length == Keyword.Length || parent == null)
				return resultCreator.SearchingByTitle();

			var currentSearch = query[Keyword.Length..];

			var results = collection.Traverse()
			                        .FilterBySettings(settings)
			                        .FuzzySearch(currentSearch, context)
			                        .Select(x => resultCreator.CreateOneNoteItemResult(x.item, false, x.highlightData, x.score))
			                        .ToList();

			return results.Any() ? results : ResultCreator.NoMatchesFound();
		}
	}
}