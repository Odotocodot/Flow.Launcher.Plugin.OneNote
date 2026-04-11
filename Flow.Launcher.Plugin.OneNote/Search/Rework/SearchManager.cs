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
			var standardSearch = new StandardSearch();
			searchFunctions = new OrderedDictionary<Keyword, SearchFunction>()
			{
				{ keywords.NotebookExplorer, new NotebookExplorer() },
				{ keywords.TitleSearch, new TitleSearch() },
				{ keywords.ScopedSearch, standardSearch },
				{ Keyword.Empty, standardSearch },
			};
		}
		
		//Get the keyword 
		private static bool TryParseSearch(ref SearchData searchData, [NotNullWhen(true)] out Keyword? keyword, IEnumerable<Keyword> keywordsToMatch, Keywords keywords)
		{
			foreach (Keyword key in keywordsToMatch)	
			{
				if (TryParse(key, ref searchData, keywords))
				{
					keyword = key;
					return true;
				}
			}

			//Should never fail;
			//context.API.LogWarn(nameof(SearchManager), $"Could not parse keywords from search: {search}. This should never happen...");
			keyword = null;
			return false;
			
			static bool TryParse(Keyword keyword, ref SearchData searchData, Keywords keywords) //TODO: Use span for performance
			{
				(var search, _,_,_) = searchData;
				if (!search.StartsWithOrd(keyword))
				{
					return false;
				}
				
				if ( /*keyword != keywords.NotebookExplorer &&*/ search[keyword.Length..].StartsWith(keywords.RecentPages)) //TODO maybe allow NotebookExplorer+ RecentPAges?
				{
					searchData.search = search[(keyword.Length + keywords.RecentPages.Length)..];
					searchData.sortOrder = SortOrder.RecentlyModified;
					return true;
				}

				searchData.search = search[keyword.Length..];
				searchData.sortOrder = SortOrder.Default;
				return true;
			}
		}
		
		public List<Result> Query(Query query)
		{
			if (query.IsReQuery)
			{
				RootCache.SetDirty();
			}

			var searchData = new SearchData(query.Search, null, RootCache.Root.Notebooks, SortOrder.Default);
			if (!TryParseSearch(ref searchData, out var keyword, searchFunctions.Keys, keywords))
				return ResultCreator.SingleResult("Failed at parsing keywords", string.Empty, string.Empty);
			
			var searchFunction = searchFunctions[keyword];
			
			var results = string.IsNullOrWhiteSpace(searchData.search) 
				? searchFunction.EmptySearch(ref searchData) 
				: searchFunction.GetResults(ref searchData);
			
			return results.Count != 0 ? results : ResultCreator.NoMatchesFound();
		}
	}

	public abstract class SearchFunction
	{
		protected ResultCreator resultCreator;
		protected Settings settings;
		protected PluginInitContext context;
		public abstract List<Result> GetResults(ref SearchData searchData);
		public abstract List<Result> EmptySearch(ref SearchData searchData);
	}

	public class StandardSearch : SearchFunction
	{
		public override List<Result> GetResults(ref SearchData searchData)
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

		public override List<Result> EmptySearch(ref SearchData searchData) => resultCreator.SearchType("Now searching all pages", searchData.parent?.Name);
	}

	public class TitleSearch : SearchFunction
	{
		public override List<Result> GetResults(ref SearchData searchData)
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

		public override List<Result> EmptySearch(ref SearchData searchData)
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
		private Explorer explorer = new Explorer();

		public override List<Result> GetResults(ref SearchData searchData)
		{
				//TODO: check if searchData.search is empty and searchData.sortOrder is recently modified
			if (!ValidateSearch(ref searchData)) 
				return resultCreator.InvalidQuery(false);

			return null;
			// if (parent == null)
			// 	return results;
			//
			// Result result = resultCreator.CreateOneNoteItemResult(parent, false, score: Result.MaxScore);
			// result.Title = $"Open \"{parent.Name}\" in OneNote";
			// result.SubTitle = $"Use \'{Keywords.ScopedSearch}\' to search this item. Use \'{Keywords.TitleSearch}\' to search by title in this item";
			// results.Add(result);
			// return results;
		}
		
		private bool ValidateSearch(ref SearchData searchData) 
		{
			(var search, IOneNoteItem? parent, IEnumerable<IOneNoteItem>? collection, _) = searchData;
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
					return false;
                
				collection = parent.Children;
                    
				prevIndex = currIndex + 1;
				currIndex = search.IndexOf(separator, currIndex + separator.Length, StringComparison.Ordinal);
			}

			lastSearch = search[prevIndex..];
			searchData.search = lastSearch;
			searchData.parent = parent;
			searchData.collection = collection;
			return true;
		}

		public override List<Result> EmptySearch(ref SearchData searchData) => explorer.EmptySearch(ref searchData);


		private class Explorer : SearchFunction
		{
			public override List<Result> GetResults(ref SearchData searchData)
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

			public override List<Result> EmptySearch(ref SearchData searchData)
			{
				Debug.Assert(searchData.collection != null);
				var results = searchData.collection.FilterBySettings(settings) 
				                        .Select(item => resultCreator.CreateOneNoteItemResult(item, true))
				                        .ToList();
			
				return results.Count != 0 ? results : resultCreator.EmptyCollection(results, searchData.parent);
			}
		}
	}
	
	public record struct SearchData(string search, IOneNoteItem? parent, IEnumerable<IOneNoteItem>? collection, SortOrder sortOrder);

	public enum SortOrder
	{
		Default = 0,
		RecentlyModified = 1,
	}
}