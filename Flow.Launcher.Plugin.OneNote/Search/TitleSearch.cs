using System.Collections.Generic;
using System.Linq;
using LinqToOneNote;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class TitleSearch(PluginInitContext context, Settings settings, ResultCreator resultCreator) : SearchBase(context, settings, resultCreator)
	{
		protected override List<Result> GetResultsInternal(ref SearchData searchData)
		{
			var (search, _, collection, sortOrder) = searchData;
			var results = collection.Descendants()
			                        .FilterBySettings(settings)
			                        .FuzzySearch(search, context);

			return sortOrder switch
			{
				SortOrder.RecentlyModified => results.OrderByDescending(sr => sr.Item.LastModified)
				                                     .Take(settings)
				                                     .Select(sr => resultCreator.CreateRecentItemResult(sr.Item, false, sr.HighlightData))
				                                     .ToList(),
				_ => results.Take(settings)
				            .Select(sr => resultCreator.CreateOneNoteItemResult(sr.Item, false, sr.HighlightData, sr.Score))
				            .ToList(),
			};
		}

		protected override List<Result> EmptySearch(ref SearchData searchData)
		{
			const string emptySearchPrefix = "Now searching by title";
			return searchData.SortOrder switch
			{
				SortOrder.RecentlyModified => resultCreator.SearchType($"{emptySearchPrefix} ordered by last modified", searchData.Parent?.Name),
				_ => resultCreator.SearchType(emptySearchPrefix, searchData.Parent?.Name),
			};
		}
	}
}