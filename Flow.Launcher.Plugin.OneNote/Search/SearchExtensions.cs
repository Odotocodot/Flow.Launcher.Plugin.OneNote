using System;
using System.Collections.Generic;
using System.Linq;
using Flow.Launcher.Plugin.SharedModels;
using LinqToOneNote;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public enum SortOrder
	{
		Default = 0,
		RecentlyModified = 1,
	}

	//Parent => parent of the current item/search area
	//Collection => children of the parent, if parent == null; Collection = rootCache.Root.Notebooks
	public record struct SearchData(string Search, IOneNoteItem? Parent, IEnumerable<IOneNoteItem>? Collection, SortOrder SortOrder);

	public record SearchResult<T>(T Item, List<int>? HighlightData, int Score) where T : IOneNoteItem;

	public static class SearchExtensions
	{
		public static IEnumerable<SearchResult<T>> FuzzySearch<T>(this IEnumerable<T> source, string search, PluginInitContext context)
			where T : IOneNoteItem
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
			return source.Where(item =>
			{
				if (!settings.ShowEncrypted && item is Section section)
				{
					return !section.Encrypted;
				}

				return settings.ShowRecycleBin || !item.IsInRecycleBin();
			});
		}

		public static IEnumerable<T> Take<T>(this IEnumerable<T> source, Settings settings) =>
			settings.UseMaxResults 
				? source.Take(settings.MaxResults)
				: source;

		/// <summary>
		/// Gets the keyword from the search, and removes it from the search string. Also gets the sort order
		/// </summary>
		/// <param name="searchData"></param>
		/// <param name="keywords"></param>
		/// <returns></returns>
		public static Keyword? GetKeyword(ref SearchData searchData, Keywords keywords)
		{
			Keyword? keyword = null;
			var searchSpan = searchData.Search.AsSpan();

			for (var i = 0; i < keywords.All.Length; i++)
			{
				Keyword key = keywords.All[i];
				if (!StartsWithKeyword(ref searchSpan, key))
					continue;

				keyword = key;
				break;
			}

			if (keyword == keywords.RecentPages || StartsWithKeyword(ref searchSpan, keywords.RecentPages))
				searchData.SortOrder = SortOrder.RecentlyModified;

			searchData.Search = searchSpan.ToString();
			return keyword;

			//Check if starts with keyword and if so trim the keyword
			static bool StartsWithKeyword(ref ReadOnlySpan<char> searchSpan, Keyword keyword)
			{
				if (searchSpan.StartsWith(keyword.Value.AsSpan(), StringComparison.Ordinal))
				{
					searchSpan = searchSpan[keyword.Length..];
					return true;
				}

				return false;
			}
		}
	}
}