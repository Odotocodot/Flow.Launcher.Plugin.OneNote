using System.Collections.Generic;
using System.Linq;
using LinqToOneNote;
using OneNoteApp = LinqToOneNote.OneNote;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class RecentPages : SearchBase
	{
		public RecentPages(PluginInitContext context, Settings settings, ResultCreator resultCreator) 
			: base(context, settings, resultCreator, settings.Keywords.RecentPages) { }

		public override List<Result> GetResults(string query)
		{
			int count = settings.DefaultRecentsCount;
			
			if (query.Length > keyword.Length && int.TryParse(query[keyword.Length..], out int userChosenCount))
				count = userChosenCount;
        
			return OneNoteApp.GetFullHierarchy()
			                 .Notebooks
	                         .GetAllPages()
	                         .FilterBySettings(settings)
	                         .OrderByDescending(pg => pg.LastModified)
	                         .Take(count)
	                         .Select(resultCreator.CreateRecentPageResult)
	                         .ToList();
		}
	}
}