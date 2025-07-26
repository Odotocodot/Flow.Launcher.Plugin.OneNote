using System.Collections.Generic;

namespace Flow.Launcher.Plugin.OneNote.Search
{
	public abstract class SearchBase
	{
		protected readonly PluginInitContext context;
		protected readonly Settings settings;
		protected readonly ResultCreator resultCreator;
		public readonly Keyword Keyword;
		protected SearchBase(PluginInitContext context, Settings settings, ResultCreator resultCreator, Keyword keyword)
		{
			this.context = context;
			this.settings = settings;
			this.resultCreator = resultCreator;
			Keyword = keyword;
		}
		protected Keywords Keywords => settings.Keywords;
		public abstract List<Result> GetResults(string query);
	}
}