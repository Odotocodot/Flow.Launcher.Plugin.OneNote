using System;
using System.Collections.Generic;
using System.Linq;
using Flow.Launcher.Plugin.SharedModels;
using LinqToOneNote;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public record SearchResult<T>(T Item, List<int>? HighlightData, int Score) where T : IOneNoteItem
	{
		public int Score { get; set; } = Score;
	}

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
				if (!settings.ShowEncrypted && item is Section section)
				{
					success = !section.Encrypted;
				}

				if (!settings.ShowRecycleBin && item.IsInRecycleBin())
				{
					success = false;
				}

				if (success)
				{
					yield return item;
				}
			}
		}

		public static IEnumerable<Result> OrderByLastModified(this IEnumerable<Result> source, bool sortByLastModified)
		{
			return sortByLastModified 
				? source.OrderByDescending(result =>
				{
					result.Score = 0;
					result.AddSelectedCount = !sortByLastModified;
					return ((IOneNoteItem)result.ContextData).LastModified;
				})
				: source;
		}
			
		public static bool StartsWithOrd(this string str, string value) => str.StartsWith(value, StringComparison.Ordinal);
	}
}