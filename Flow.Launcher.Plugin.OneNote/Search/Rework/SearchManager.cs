using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LinqToOneNote;
using LinqToOneNote.Abstractions;
using OneNoteApp = LinqToOneNote.OneNote;

namespace Flow.Launcher.Plugin.OneNote.Search.Rework
{
	//TODO: sort and switch
	// for sort pass in through SearchData
	public class SearchManager
	{
		private readonly OrderedDictionary<Keyword, SearchFunction> searchFunctions; //TODO: mutable keys beware/ need to fix
		private readonly Keywords keywords;
		private readonly PluginInitContext context;
		public RootCache RootCache { get; }  = new RootCache();

		public SearchManager(PluginInitContext context, Settings settings, ResultCreator resultCreator)
		{
			keywords = settings.Keywords;
			this.context = context;
			searchFunctions = new OrderedDictionary<Keyword, SearchFunction>()
			{
				{ keywords.NotebookExplorer, new NotebookExplorer() },
				{ keywords.TitleSearch, new TitleSearch() },
				{ keywords.ScopedSearch, new StandardSearch() },
				{ Keyword.Empty, new StandardSearch() },
			};
		}
		
		private bool TryParseKeywords(string search, out (Keyword keyword, SearchData searchData) data)
		{
			foreach (Keyword keyword in searchFunctions.Keys)	
			{
				if (TryParse(keyword, out var searchData))
				{
					data = (keyword, searchData.Value);
					return true;
				}
			}

			//Should never fail;
			//context.API.LogWarn(nameof(SearchManager), $"Could not parse keywords from search: {search}. This should never happen...");
			data = default;
			return false;
			
			bool TryParse(Keyword keyword, [NotNullWhen(true)] out SearchData? searchData) //TODO: Use span for performance
			{
				if (!search.StartsWithOrd(keyword))
				{
					searchData = null;
					return false;
				}
				
				if ( /*keyword != keywords.NotebookExplorer &&*/ search[keyword.Length..].StartsWith(keywords.RecentPages)) //TODO maybe allow NotebookExplorer+ RecentPAges?
				{
					searchData = new SearchData(
						search[(keyword.Length + keywords.RecentPages.Length)..],
						null,
						RootCache.Root.Notebooks,
						SortOrder.RecentlyModified
					);
					return true;
				}

				searchData = new SearchData(search[keyword.Length..], null, RootCache.Root.Notebooks, SortOrder.Default);
				return true;
			}
		}
		
		public List<Result> Query(Query query)
		{
			if (query.IsReQuery)
			{
				RootCache.SetDirty();
			}

			if (!TryParseKeywords(query.Search, out var data))
				return ResultCreator.SingleResult("Failed at parsing keywords", string.Empty, string.Empty);
			
			var (keyword, searchData) = data;
			var searchFunction = searchFunctions[keyword];
			
			var results = string.IsNullOrWhiteSpace(searchData.search) 
				? searchFunction.EmptySearch(in searchData) 
				: searchFunction.GetResults(in searchData);
			
			return results.Count != 0 ? results : ResultCreator.NoMatchesFound();
		}
	}

	public abstract class SearchFunction
	{
		protected ResultCreator resultCreator;
		protected Settings settings;
		protected PluginInitContext context;
		public abstract List<Result> GetResults(in SearchData searchData);
		public abstract List<Result> EmptySearch(in SearchData searchData);
	}

	public class StandardSearch : SearchFunction
	{
		public override List<Result> GetResults(in SearchData searchData)
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
				                                   .Select(pg => resultCreator.CreateRecentItemResult(pg))
				                                   .ToList(),
				_ => pages.Select(pg => resultCreator.CreatePageResult(pg, search)).ToList()
			};
		}

		public override List<Result> EmptySearch(in SearchData searchData) => resultCreator.SearchType("Now searching all pages", searchData.parent?.Name);
	}

	public class TitleSearch : SearchFunction
	{
		public override List<Result> GetResults(in SearchData searchData)
		{
			var (search, _, collection, sortOrder) = searchData;
			var results = collection.Descendants()
			                        .FilterBySettings(settings)
			                        .FuzzySearch(search, context);

			return sortOrder switch
			{
				SortOrder.RecentlyModified => results.OrderByDescending(sr => sr.Item.LastModified)
				                                     .Select(x => resultCreator.CreateRecentItemResult(x.Item, x.HighlightData, x.Score))
				                                     .ToList(),
				_ => results.Select(x => resultCreator.CreateOneNoteItemResult(x.Item, false, x.HighlightData, x.Score))
				            .ToList(),
			};
		}

		public override List<Result> EmptySearch(in SearchData searchData)
		{
			const string emptySearchPrefix = "Now searching by title";
			return searchData.sortOrder switch
			{
				SortOrder.RecentlyModified => resultCreator.SearchType($"{emptySearchPrefix} ordered by last modified", searchData.parent?.Name),
				_ => resultCreator.SearchType(emptySearchPrefix, searchData.parent?.Name),
			};
		}
	}

	public class NotebookExplorer : SearchFunction
	{
		private Explorer explorer;

		public override List<Result> GetResults(in SearchData searchData)
		{
				//TODO: check if searchData.search is empty and searchData.sortOrder is recently modified
			if (ValidateSearch(in searchData, out SearchData data)) 
				return resultCreator.InvalidQuery(false);
			
			
			// if (parent == null)
			// 	return results;
			//
			// Result result = resultCreator.CreateOneNoteItemResult(parent, false, score: Result.MaxScore);
			// result.Title = $"Open \"{parent.Name}\" in OneNote";
			// result.SubTitle = $"Use \'{Keywords.ScopedSearch}\' to search this item. Use \'{Keywords.TitleSearch}\' to search by title in this item";
			// results.Add(result);
			// return results;
		}
		
		private bool ValidateSearch(in SearchData inData, out SearchData searchData) 
		{
			(var search, IOneNoteItem? parent, IEnumerable<IOneNoteItem>? collection, _) = inData;
			Debug.Assert(collection != null, nameof(collection) + " != null");
			//parent is null here
			//collection is rootCache.Root.Notebooks as this functions validates the search with the whole query;
			
			string lastSearch = string.Empty;
			
			const string separator = Keywords.NotebookExplorerSeparator;
			var currIndex = search.IndexOf(separator, StringComparison.Ordinal);
			var prevIndex = 0;
			
			while (currIndex != -1)
			{
				var itemName = search[prevIndex..currIndex];
				parent = collection.FirstOrDefault(item => item.Name == itemName);
				if (parent == null)
				{
					searchData = default;
					return false;
				}
                
				collection = parent.Children;
                    
				prevIndex = currIndex + 1;
				currIndex = search.IndexOf(separator, currIndex + separator.Length, StringComparison.Ordinal);
			}

			lastSearch = search[prevIndex..];
			searchData = inData with { search = lastSearch, parent = parent, collection = collection }; 
			return true;
		}

		public override List<Result> EmptySearch(in SearchData searchData) => explorer.EmptySearch(in searchData);


		private class Explorer : SearchFunction
		{
			public override List<Result> GetResults(in SearchData searchData)
			{
				var (search, parent, collection, sortOrder) = searchData; //TODO: sort order
				
				var results = collection.FilterBySettings(settings)
				                        .FuzzySearch(search, context)
				                        .Select(r => resultCreator.CreateOneNoteItemResult(r.Item, true, r.HighlightData, r.Score))
				                        .ToList();

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

			public override List<Result> EmptySearch(in SearchData searchData)
			{
				Debug.Assert(searchData.collection != null);
				var results = searchData.collection.FilterBySettings(settings) 
				                        .Select(item => resultCreator.CreateOneNoteItemResult(item, true))
				                        .ToList();
			
				return results.Count != 0 ? results : resultCreator.EmptyCollection(results, searchData.parent);
			}
		}
	}
	
	public readonly record struct SearchData(string search, IOneNoteItem? parent, IEnumerable<IOneNoteItem>? collection, SortOrder sortOrder);

	public enum SortOrder
	{
		Default = 0,
		RecentlyModified = 1,
	}
}