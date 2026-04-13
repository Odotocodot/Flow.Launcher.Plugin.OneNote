using System.Collections.Generic;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public abstract class SearchBase(PluginInitContext context, Settings settings, ResultCreator resultCreator)
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
}