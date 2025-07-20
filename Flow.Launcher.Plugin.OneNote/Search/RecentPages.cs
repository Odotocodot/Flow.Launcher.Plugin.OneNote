using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class RecentPages : SearchBase
	{
		public override List<Result> GetResults(string query)
		{
			int count = settings.DefaultRecentsCount;
			
			if (query.Length > Keyword.Length && int.TryParse(query[Keyword.Length..], out int userChosenCount))
				count = userChosenCount;
        
			return OneNoteApplication.GetNotebooks()
			                         .GetPages()
			                         .FilterBySettings(settings)
			                         .OrderByDescending(pg => pg.LastModified)
			                         .Take(count)
			                         .Select(resultCreator.CreateRecentPageResult)
			                         .ToList();
		}
	}
}