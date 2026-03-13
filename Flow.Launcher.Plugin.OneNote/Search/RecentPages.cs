using System.Collections.Generic;
using System.Linq;
using LinqToOneNote;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class RecentPages(PluginInitContext context, Settings settings, ResultCreator resultCreator, RootCache rootCache)
		: SearchBase(context, settings, resultCreator, settings.Keywords.RecentPages)
	{
		public override List<Result> GetResults(Query query)
		{
			int count = settings.DefaultRecentsCount;
			string search = query.Search;
			if (search.Length > keyword.Length && int.TryParse(search[keyword.Length..], out int userChosenCount))
				count = userChosenCount;
        
			return rootCache.Root
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