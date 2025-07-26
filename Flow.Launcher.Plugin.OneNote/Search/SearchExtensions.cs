using System;
using System.Collections.Generic;
using Flow.Launcher.Plugin.SharedModels;
using Odotocodot.OneNote.Linq;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public record struct SearchResult<T>(T item, List<int>? highlightData, int score) where T : IOneNoteItem;
	public static class SearchExtensions
	{
		public static IEnumerable<SearchResult<T>> FuzzySearch<T>(this IEnumerable<T> source, string search, PluginInitContext context) where T: IOneNoteItem
		{
			foreach (var item in source)
			{
				MatchResult match = context.API.FuzzySearch(search, item.Name);
				if (match.IsSearchPrecisionScoreMet())
				{
					yield return new SearchResult<T>(item, match.MatchData, match.Score);
				}
			}
		}
		public static IEnumerable<T> FilterBySettings<T>(this IEnumerable<T> source, Settings settings) where T : IOneNoteItem
		{
			foreach (var item in source)
			{
				var success = true;
				if (settings.ShowEncrypted && item is OneNoteSection section)
				{
					success = !section.Encrypted;
				}

				if (settings.ShowRecycleBin && item.IsInRecycleBin())
				{
					success = false;
				}

				if (success)
				{
					yield return item;
				}
			}
		}

		public static bool StartsWithOrd(this string str, string value)
		{
			return str.StartsWith(value, StringComparison.Ordinal);
		}
	}
}