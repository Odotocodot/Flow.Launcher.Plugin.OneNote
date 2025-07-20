using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class TitleSearch : SearchBase
	{
		public override List<Result> GetResults(string query)
		{
			return Filter(query, null, OneNoteApplication.GetNotebooks());
		}
		
		public List<Result> Filter(string query, IOneNoteItem? parent, IEnumerable<IOneNoteItem> collection)
		{
			if (query.Length == Keyword.Length && parent == null)
				return resultCreator.SearchingByTitle();
			
			var currentSearch = query[Keyword.Length..];

			return collection.Traverse()
			                 .FilterBySettings(settings)
			                 .FuzzySearch(currentSearch, context)
			                 .Select(x => resultCreator.CreateOneNoteItemResult(x.item, false, x.highlightData, x.score))
			                 .ToList();
		}
		
		public static List<Result> Filter(string query, IOneNoteItem? parent, IEnumerable<IOneNoteItem> collection, PluginInitContext context, Settings settings, ResultCreator resultCreator)
		{
			if (query.Length == settings.Keywords.TitleSearch.Length && parent == null)
				return resultCreator.SearchingByTitle();
			
			var currentSearch = query[settings.Keywords.TitleSearch.Length..];

			return collection.Traverse()
			                 .FilterBySettings(settings)
			                 .FuzzySearch(currentSearch, context)
			                 .Select(x => resultCreator.CreateOneNoteItemResult(x.item, false, x.highlightData, x.score))
			                 .ToList();
		}
	}
}