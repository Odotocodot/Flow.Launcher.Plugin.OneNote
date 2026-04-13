using System.Collections.Generic;
using System.Linq;
using LinqToOneNote;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class StandardSearch(PluginInitContext context, Settings settings, ResultCreator resultCreator) : SearchBase(context, settings, resultCreator)
	{
		protected override List<Result> GetResultsInternal(ref SearchData searchData)
		{
			var (search, parent, _, sortOrder) = searchData;
			
			if (!char.IsLetterOrDigit(search[0]))
				return resultCreator.InvalidQuery();
			
			IEnumerable<Page> pages = parent is null 
				? LinqToOneNote.OneNote.FindPages(search) 
				: LinqToOneNote.OneNote.FindPages(search, parent);

			return sortOrder switch
			{
				SortOrder.RecentlyModified => pages.OrderByDescending(pg => pg.LastModified)
				                                   .Select(pg => resultCreator.CreateRecentItemResult(pg, false))
				                                   .ToList(),
				_ => pages.Select(pg => resultCreator.CreateOneNoteItemResult(pg, false,
					          string.IsNullOrWhiteSpace(search)
						          ? null
						          : context.API.FuzzySearch(search, pg.Name).MatchData))
				          .ToList()
			};
		}

		protected override List<Result> EmptySearch(ref SearchData searchData)
		{
			return searchData.SortOrder switch
			{
				SortOrder.RecentlyModified => searchData.Collection
				                                        .GetAllPages()
				                                        .FilterBySettings(settings)
				                                        .OrderByDescending(pg => pg.LastModified)
				                                        .Select(pg => resultCreator.CreateRecentItemResult(pg, false))
				                                        .ToList(),
				_ => resultCreator.SearchType("Now searching all pages", searchData.Parent?.Name),
			};
		}
	}
}