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
	public class SearchManager
	{
		private readonly OrderedDictionary<Keyword, SearchFunction> searchFunctions; //TODO: mutable keys beware/ need to fix
		private readonly Keywords keywords;
		private readonly PluginInitContext context;
		public RootCache RootCache { get; }

		public SearchManager(PluginInitContext context, Settings settings, ResultCreator resultCreator)
		{
			var standardSearch = new StandardSearch(context, settings, resultCreator);
			var titleSearch = new TitleSearch(context, settings, resultCreator);
			
			RootCache = new RootCache();
			keywords = settings.Keywords;
			
			searchFunctions = new OrderedDictionary<Keyword, SearchFunction>()
			{
				{ keywords.NotebookExplorer, new NotebookExplorer(context, settings, resultCreator, titleSearch, standardSearch) },
				{ keywords.TitleSearch, titleSearch },
				{ Keyword.Empty, standardSearch },
			};
		}
		
		//Get the keyword 
		public static bool TryParseSearch(ref SearchData searchData, [NotNullWhen(true)] out Keyword? keyword, IList<Keyword> keywordsToMatch, Keywords keywords)
		{
			var search = searchData.Search;
			for (var i = 0; i < keywordsToMatch.Count; i++)
			{
				Keyword key = keywordsToMatch[i];
				if (!search.StartsWithOrd(key))
					continue;

				keyword = key;
				if (keyword != keywords.NotebookExplorer && search[key.Length..].StartsWith(keywords.RecentPages)) //TODO maybe allow NotebookExplorer+ RecentPAges?
				{
					searchData.Search = search[(key.Length + keywords.RecentPages.Length)..];
					searchData.SortOrder = SortOrder.RecentlyModified;
					return true;
				}

				searchData.Search = search[key.Length..];
				searchData.SortOrder = SortOrder.Default;
				return true;
			}

			//Should never fail;
			//context.API.LogWarn(nameof(SearchManager), $"Could not parse keywords from search: {search}. This should never happen...");
			keyword = null;
			return false;
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
			
			var results = string.IsNullOrWhiteSpace(searchData.Search) 
				? searchFunction.EmptySearch(ref searchData) 
				: searchFunction.GetResults(ref searchData);
			
			return results.Count != 0 ? results : ResultCreator.NoMatchesFound();
		}
	}

	public abstract class SearchFunction(PluginInitContext context, Settings settings, ResultCreator resultCreator)
	{
		protected readonly ResultCreator resultCreator = resultCreator;
		protected readonly Settings settings = settings;
		protected readonly PluginInitContext context = context;
		public abstract List<Result> GetResults(ref SearchData searchData);
		public abstract List<Result> EmptySearch(ref SearchData searchData);
	}

	public class StandardSearch(PluginInitContext context, Settings settings, ResultCreator resultCreator) : SearchFunction(context, settings, resultCreator)
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
				                                   .Select(pg => resultCreator.CreateRecentItemResult(pg, false))
				                                   .ToList(),
				_ => pages.Select(pg => resultCreator.CreatePageResult(pg, search)).ToList()
			};
		}

		public override List<Result> EmptySearch(ref SearchData searchData)
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
		public override List<Result> GetResults(ref SearchData searchData)
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

		public override List<Result> EmptySearch(ref SearchData searchData)
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
		private readonly Keyword[] keywordsToMatch = [settings.Keywords.TitleSearch, settings.Keywords.ScopedSearch, Keyword.Empty];

		public override List<Result> GetResults(ref SearchData searchData)
		{
			if (!ValidateSearch(ref searchData)) 
				return resultCreator.InvalidQuery(false);

			if(!SearchManager.TryParseSearch(ref searchData, out Keyword? keyword, keywordsToMatch, keywords))
				return ResultCreator.SingleResult("Failed at parsing keywords", string.Empty, string.Empty);
			
			IOneNoteItem? parent = searchData.Parent;
			SearchFunction searchFunction = (keyword, parent) switch
			{
				(not null, not Page) when keyword == keywords.TitleSearch => titleSearch,
				(not null, INotebookOrSectionGroup) when keyword == keywords.ScopedSearch => standardSearch,
				_ => explorer,
			};

			var results = string.IsNullOrWhiteSpace(searchData.Search)
				? searchFunction.EmptySearch(ref searchData)
				: searchFunction.GetResults(ref searchData);

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

		public override List<Result> EmptySearch(ref SearchData searchData) => explorer.EmptySearch(ref searchData);


		private class Explorer(PluginInitContext context, Settings settings, ResultCreator resultCreator) : SearchFunction(context, settings, resultCreator)
		{
			public override List<Result> GetResults(ref SearchData searchData)
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

			public override List<Result> EmptySearch(ref SearchData searchData)
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