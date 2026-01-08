using System.Collections.Generic;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public abstract class SearchBase
	{
		protected readonly PluginInitContext context;
		protected readonly Settings settings;
		protected readonly ResultCreator resultCreator;
#nullable disable
		public readonly Keyword keyword;
#nullable restore
		protected SearchBase(PluginInitContext context, Settings settings, ResultCreator resultCreator, Keyword? keyword)
		{
			this.context = context;
			this.settings = settings;
			this.resultCreator = resultCreator;
			this.keyword = keyword;
		}
		protected Keywords Keywords => settings.Keywords;
		public abstract List<Result> GetResults(string query);
	}
}