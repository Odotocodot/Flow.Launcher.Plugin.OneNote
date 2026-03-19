using System.Collections.Generic;
using System.Linq;
using LinqToOneNote;
using OneNoteApp = LinqToOneNote.OneNote;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class StandardSearch(PluginInitContext context, Settings settings, ResultCreator resultCreator)
		: SearchBase(context, settings, resultCreator, Keyword.Empty)
	{
		//Standard search
		public override List<Result> GetResults(Query query) => Filter(query.Search, null, keyword);
		//Scope search
		public List<Result> GetResults(Query query, IOneNoteItem? parent) => Filter(query.Search, parent, settings.Keywords.ScopedSearch);

		private List<Result> Filter(string query, IOneNoteItem? parent, Keyword keyword)
		{
			if (query.Length == keyword.Length)
				return resultCreator.SearchType("Now searching all pages", parent?.Name);
			
			if (!char.IsLetterOrDigit(query[keyword.Length]))
				return resultCreator.InvalidQuery();
			
			string currentSearch = query[keyword.Length..];
			
			IEnumerable<Page> pages = parent is null 
				? OneNoteApp.FindPages(currentSearch) 
				: OneNoteApp.FindPages(currentSearch, parent);
			var results = pages.Select(pg => resultCreator.CreatePageResult(pg, currentSearch))
			                   .ToList();
			
			return results.Count != 0 ? results : ResultCreator.NoMatchesFound();
		}

	}
}