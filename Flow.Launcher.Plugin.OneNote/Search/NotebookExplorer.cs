using System;
using System.Collections.Generic;
using System.Linq;
using Odotocodot.OneNote.Linq;
using Odotocodot.OneNote.Linq.Abstractions;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public class NotebookExplorer : SearchBase
	{
		public override List<Result> GetResults(string query)
		{
			if (ValidateSearch(query, out string? search, out IOneNoteItem? parent, out IEnumerable<IOneNoteItem> collection))
				return resultCreator.InvalidQuery(false);

			List<Result> results = search switch
			{
				// Empty search so show all in collection
				string when string.IsNullOrWhiteSpace(search) => ShowAll(parent, collection),

				// Search by title
				not null when search.StartsWith(Keywords.TitleSearch) && parent is not OneNotePage 
					=> TitleSearch.Filter(search, parent, collection, context, settings, resultCreator),

				// Scoped search
				not null when search.StartsWith(Keywords.ScopedSearch) && parent is INotebookOrSectionGroup => ScopedSearch(search, parent),

				// Default search
				_ => Explorer(search, parent, collection),
			};

			if (parent == null)
				return results;
			
			Result result = resultCreator.CreateOneNoteItemResult(parent, false, score: Result.MaxScore);
			result.Title = $"Open \"{parent.Name}\" in OneNote";
			result.SubTitle = search switch
			{
				not null when search.StartsWith(Keywords.TitleSearch) => $"Now searching by title in \"{parent.Name}\"",
				not null when search.StartsWith(Keywords.ScopedSearch) => $"Now searching all pages in \"{parent.Name}\"",
				_ => $"Use \'{Keywords.ScopedSearch}\' to search this item. Use \'{Keywords.TitleSearch}\' to search by title in this item",
			};

			results.Add(result);
			return results;
		}

		private bool ValidateSearch(string query, out string? lastSearch, out IOneNoteItem? parent, out IEnumerable<IOneNoteItem> collection)
		{
			lastSearch = null;
			parent = null;
			collection = OneNoteApplication.GetNotebooks();
			
			string search = query[(query.IndexOf(Keywords.NotebookExplorer, StringComparison.Ordinal) + Keywords.NotebookExplorer.Length)..];
			
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
			return true;
		}
		
		private List<Result> ShowAll(IOneNoteItem? parent, IEnumerable<IOneNoteItem> collection)
		{
			var results = collection.FilterBySettings(settings) 
			                        .Select(item => resultCreator.CreateOneNoteItemResult(item, true))
			                        .ToList();
			
			return results.Any() ? results : resultCreator.NoItemsInCollection(results, parent);
		}
		
		private List<Result> ScopedSearch(string query, IOneNoteItem parent)
		{
			if (query.Length == Keywords.ScopedSearch.Length)
				return new List<Result>(0);

			if (!char.IsLetterOrDigit(query[Keywords.ScopedSearch.Length]))
				return resultCreator.InvalidQuery();

			string currentSearch = query[Keywords.TitleSearch.Length..];

			return OneNoteApplication.FindPages(currentSearch, parent)
			                         .Select(pg => resultCreator.CreatePageResult(pg, currentSearch))
			                         .ToList();
		}
		
		private List<Result> Explorer(string search, IOneNoteItem? parent, IEnumerable<IOneNoteItem> collection)
		{
			var results = collection.FilterBySettings(settings)
			                        .FuzzySearch(search, context)
			                        .Select(r => resultCreator.CreateOneNoteItemResult(r.item, true, r.highlightData, r.score))
			                        .ToList();

			// If parent is a section, pages inside can have the same name
			if (parent is not OneNoteSection && results.Any(result => string.Equals(search.Trim(), result.Title, StringComparison.OrdinalIgnoreCase)))
				return results;

			if (parent?.IsInRecycleBin() == true)
				return results;

			//Add option to create new items
			switch (parent)
			{
				case null:
					results.Add(resultCreator.CreateNewNotebookResult(search));
					break;
				case INotebookOrSectionGroup:
					results.Add(resultCreator.CreateNewSectionResult(search, parent));
					results.Add(resultCreator.CreateNewSectionGroupResult(search, parent));
					break;
				case OneNoteSection section:
					if (!section.Locked)
					{
						results.Add(resultCreator.CreateNewPageResult(search, section));
					}
					break;
			}

			return results;
		}
	}
}