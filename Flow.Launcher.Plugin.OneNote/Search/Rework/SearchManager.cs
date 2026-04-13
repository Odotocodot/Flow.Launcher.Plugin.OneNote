using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LinqToOneNote;
using LinqToOneNote.Abstractions;
using OneNoteApp = LinqToOneNote.OneNote;

namespace Flow.Launcher.Plugin.OneNote.Search.Rework
{
	public class SearchManager
	{
		private readonly Keywords keywords;
		private readonly NotebookExplorer notebookExplorer;
		private readonly StandardSearch standardSearch;
		private readonly TitleSearch titleSearch;
		public RootCache RootCache { get; }

		public SearchManager(PluginInitContext context, Settings settings, ResultCreator resultCreator)
		{
			standardSearch = new StandardSearch(context, settings, resultCreator);
			titleSearch = new TitleSearch(context, settings, resultCreator);
			notebookExplorer = new NotebookExplorer(context, settings, resultCreator, titleSearch, standardSearch);
			
			RootCache = new RootCache();
			keywords = settings.Keywords;
		}
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
			
			for (var i = 0; i < keywords.ActionKeywords.Length; i++)
			{
				Keyword key = keywords.ActionKeywords[i];
				if (!StartsWithKeyword(ref searchSpan, key))
					continue;
				
				keyword = key;
				break;
			}

			searchData.SortOrder = keyword == keywords.RecentPages || StartsWithKeyword(ref searchSpan, keywords.RecentPages)
				? SortOrder.RecentlyModified
				: SortOrder.Default;

			searchData.Search = searchSpan.ToString();
			return keyword;

			static bool StartsWithKeyword(ref ReadOnlySpan<char> searchSpan, Keyword keyword) //TODO: make extension method
			{
				if (searchSpan.StartsWith(keyword.Value.AsSpan(), StringComparison.Ordinal))
				{
					searchSpan = searchSpan[keyword.Length..];
					return true;
				}
				return false;
			}
		}
		
		public List<Result> Query(Query query)
		{
			if (query.IsReQuery)
			{
				RootCache.SetDirty();
			}

			var searchData = new SearchData(query.Search, null, RootCache.Root.Notebooks, SortOrder.Default);

			Keyword? keyword = GetKeyword(ref searchData, keywords);

			SearchFunction searchFunction = keyword switch
			{
				not null when keyword == keywords.NotebookExplorer => notebookExplorer,
				not null when keyword == keywords.TitleSearch => titleSearch,
				_ => standardSearch,
			};
			
			var results = searchFunction.GetResults(ref searchData);
			
			return results.Count != 0 ? results : ResultCreator.NoMatchesFound();
		}
	}

	public abstract class SearchFunction(PluginInitContext context, Settings settings, ResultCreator resultCreator)
	{
		protected readonly ResultCreator resultCreator = resultCreator;
		protected readonly Settings settings = settings;
		protected readonly PluginInitContext context = context;
		protected abstract List<Result> GetResultsInternal(ref SearchData searchData);
		protected abstract List<Result> EmptySearch(ref SearchData searchData); 
		
		public List<Result> GetResults(ref SearchData searchData) =>
			string.IsNullOrWhiteSpace(searchData.Search)
				? EmptySearch(ref searchData)
				: GetResultsInternal(ref searchData);
	}

	public class StandardSearch(PluginInitContext context, Settings settings, ResultCreator resultCreator) : SearchFunction(context, settings, resultCreator)
	{
		protected override List<Result> GetResultsInternal(ref SearchData searchData)
		{
			var (search, parent, _, sortOrder) = searchData;
			
			if (!char.IsLetterOrDigit(search[0]))
				return resultCreator.InvalidQuery();
			
			IEnumerable<Page> pages = parent is null 
				? OneNoteApp.FindPages(search) 
				: OneNoteApp.FindPages(search, parent);

			return sortOrder switch
			{
				SortOrder.RecentlyModified => pages.OrderByDescending(pg => pg.LastModified)
				                                   .Select(pg => resultCreator.CreateRecentItemResult(pg, false))
				                                   .ToList(),
				_ => pages.Select(pg => resultCreator.CreatePageResult(pg, search)).ToList()
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

	public class TitleSearch(PluginInitContext context, Settings settings, ResultCreator resultCreator) : SearchFunction(context, settings, resultCreator)
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
				                                     .Select(sr => resultCreator.CreateRecentItemResult(sr.Item, false, sr.HighlightData))
				                                     .ToList(),
				_ => results.Select(sr => resultCreator.CreateOneNoteItemResult(sr.Item, false, sr.HighlightData, sr.Score))
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

	public class NotebookExplorer(PluginInitContext context, Settings settings, ResultCreator resultCreator, TitleSearch titleSearch, StandardSearch standardSearch)
		: SearchFunction(context, settings, resultCreator)
	{
		private readonly Explorer explorer = new(context, settings, resultCreator);
		private readonly Keywords keywords = settings.Keywords;

		protected override List<Result> GetResultsInternal(ref SearchData searchData)
		{
			if (!ValidateSearch(ref searchData)) 
				return resultCreator.InvalidQuery(false);

			Keyword? keyword = SearchManager.GetKeyword(ref searchData, keywords);
			
			IOneNoteItem? parent = searchData.Parent;
			SearchFunction searchFunction = (keyword, parent) switch
			{
				(not null, not Page) when keyword == keywords.TitleSearch => titleSearch,
				(not null, INotebookOrSectionGroup) when keyword == keywords.ScopedSearch => standardSearch,
				_ => explorer,
			};

			var results = searchFunction.GetResults(ref searchData);

			if (parent == null)
				return results;
			
			Result result = resultCreator.CreateOneNoteItemResult(parent, false, score: Result.MaxScore);
			result.Title = $"Open \"{parent.Name}\" in OneNote";
			result.SubTitle = $"Use \'{keywords.ScopedSearch}\' to search this item. Use \'{keywords.TitleSearch}\' to search by title in this item. Use \'{keywords.RecentPages}\' to sort by recently modified";
			results.Add(result);
			return results;
		}
		
		private static bool ValidateSearch(ref SearchData searchData) 
		{
			(var search, IOneNoteItem? parent, IEnumerable<IOneNoteItem>? collection, _) = searchData;
			
			//From SearchManager.Query SearchData
			//parent is null here
			//collection is rootCache.Root.Notebooks as this functions validates the search with the whole query;

			const string separator = Keywords.NotebookExplorerSeparator;
			var currIndex = search.IndexOf(separator, StringComparison.Ordinal);
			var prevIndex = 0;
			
			while (currIndex != -1)
			{
				var itemName = search[prevIndex..currIndex];
				parent = collection?.FirstOrDefault(item => item.Name == itemName);
				if (parent == null)
					return false;
                
				collection = parent.Children;
                    
				prevIndex = currIndex + 1;
				currIndex = search.IndexOf(separator, currIndex + separator.Length, StringComparison.Ordinal);
			}

			searchData.Search = search[prevIndex..];
			searchData.Parent = parent;
			searchData.Collection = collection;
			return true;
		}

		protected override List<Result> EmptySearch(ref SearchData searchData) => explorer.GetResults(ref searchData);


		private class Explorer(PluginInitContext context, Settings settings, ResultCreator resultCreator) : SearchFunction(context, settings, resultCreator)
		{
			protected override List<Result> GetResultsInternal(ref SearchData searchData)
			{
				var (search, parent, collection, sortOrder) = searchData; 

				Debug.Assert(collection != null, nameof(collection) + " != null");
				
				var searchResults = collection.FilterBySettings(settings).FuzzySearch(search, context);
				var results = sortOrder switch
				{
					SortOrder.RecentlyModified => searchResults.OrderByDescending(sr => sr.Item.LastModified)
					                                           .Select(sr => resultCreator.CreateRecentItemResult(sr.Item, true, sr.HighlightData))
					                                           .ToList(),
					_ => searchResults.Select(sr => resultCreator.CreateOneNoteItemResult(sr.Item, true, sr.HighlightData, sr.Score))
					                  .ToList(),
				};

				// If parent is a section, pages inside can have the same name
				if (parent is not Section && results.Any(result => string.Equals(search.Trim(), result.Title, StringComparison.OrdinalIgnoreCase)))
					return results;

				if (parent?.IsInRecycleBin() == true)
					return results;

				//Add option to create new items
				switch (parent)
				{
					case null:
						results.Add(resultCreator.CreateNewNotebookResult(search));
						break;
					case INotebookOrSectionGroup x:
						results.Add(resultCreator.CreateNewSectionResult(search, x));
						results.Add(resultCreator.CreateNewSectionGroupResult(search, x));
						break;
					case Section section:
						if (!section.Locked)
						{
							results.Add(resultCreator.CreateNewPageResult(search, section));
						}
						break;
				}

				return results;
			}

			protected override List<Result> EmptySearch(ref SearchData searchData)
			{
				var (_, parent, collection, sortOrder) = searchData;
				
				Debug.Assert(collection != null);
				var results = sortOrder switch
				{
					SortOrder.RecentlyModified => collection.FilterBySettings(settings)
					                                        .OrderByDescending(item => item.LastModified)
					                                        .Select(item => resultCreator.CreateRecentItemResult(item, true))
					                                        .ToList(),
					_ => collection.FilterBySettings(settings)
					               .Select(item => resultCreator.CreateOneNoteItemResult(item, true))
					               .ToList(),
				};
			
				return results.Count != 0 ? results : resultCreator.EmptyCollection(results, parent);
			}
		}
	}
	//Parent => parent of the current item/search area
	//Collection => children of the parent, if parent == null; Collection = rootCache.Root.Notebooks
	public record struct SearchData(string Search, IOneNoteItem? Parent, IEnumerable<IOneNoteItem>? Collection, SortOrder SortOrder);

	public enum SortOrder
	{
		Default = 0,
		RecentlyModified = 1,
	}
}