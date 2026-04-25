using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LinqToOneNote;
using LinqToOneNote.Abstractions;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class NotebookExplorer(PluginInitContext context, Settings settings, ResultCreator resultCreator, TitleSearch titleSearch, StandardSearch standardSearch)
		: SearchBase(context, settings, resultCreator)
	{
		private readonly Explorer explorer = new(context, settings, resultCreator);
		private readonly Keywords keywords = settings.Keywords;

		protected override List<Result> GetResultsInternal(ref SearchData searchData)
		{
			if (!ValidateSearch(ref searchData)) 
				return resultCreator.InvalidQuery(false);

			Keyword? keyword = SearchExtensions.GetKeyword(ref searchData, keywords);
			
			IOneNoteItem? parent = searchData.Parent;
			SearchBase searchBase = (keyword, parent) switch
			{
				(not null, not Page) when keyword == keywords.TitleSearch => titleSearch,
				(not null, INotebookOrSectionGroup) when keyword == keywords.ScopedSearch => standardSearch,
				_ => explorer,
			};

			var results = searchBase.GetResults(ref searchData);

			if (parent == null)
				return results;
			
			Result result = resultCreator.CreateOneNoteItemResult(parent, false, score: Result.MaxScore);
			result.Title = $"Open \"{parent.Name}\" in OneNote";
			result.SubTitle = $"Use \'{keywords.ScopedSearch}\' to search this item. Use \'{keywords.TitleSearch}\' to search by title in this item. Use \'{keywords.SortByLastModified}\' to sort by recently modified";
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
		
		private class Explorer(PluginInitContext context, Settings settings, ResultCreator resultCreator) : SearchBase(context, settings, resultCreator)
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
}